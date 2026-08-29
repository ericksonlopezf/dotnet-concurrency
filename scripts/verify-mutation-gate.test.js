// Copyright © Erickson Lopez. MIT License.
const assert = require('assert');
const {
  loadThresholds,
  parseScoreFromDescription,
  evaluateScore,
  verifyMutationGate,
  MAX_REPORT_AGE_DAYS,
} = require('./verify-mutation-gate.js');

console.log('Running verify-mutation-gate unit tests...\n');

// Test 1: loadThresholds defaults
{
  const thresholds = loadThresholds(__dirname);
  assert.strictEqual(typeof thresholds.high, 'number');
  assert.strictEqual(typeof thresholds.low, 'number');
  assert.strictEqual(typeof thresholds.break, 'number');
  assert.strictEqual(thresholds.break, 95);
  assert.strictEqual(thresholds.low, 98);
  assert.strictEqual(thresholds.high, 100);
  console.log('✅ Test 1 Passed: loadThresholds correctly loads thresholds from stryker-config.json');
}

// Test 2: parseScoreFromDescription
{
  assert.strictEqual(parseScoreFromDescription('Score: 100% (High)'), 100);
  assert.strictEqual(parseScoreFromDescription('Mutation score: 98.5% - passed'), 98.5);
  assert.strictEqual(parseScoreFromDescription('Stryker: 95.00% (190/200 killed) - 🟠 WARNING'), 95.00);
  assert.strictEqual(parseScoreFromDescription('No score here'), null);
  assert.strictEqual(parseScoreFromDescription(null), null);
  console.log('✅ Test 2 Passed: parseScoreFromDescription handles diverse formats');
}

// Test 3: evaluateScore
{
  const thresholds = { high: 100, low: 98, break: 95 };

  const r100 = evaluateScore(100, thresholds);
  assert.strictEqual(r100.passedBreak, true);
  assert.strictEqual(r100.status, '✅ HIGH');

  const r99 = evaluateScore(99, thresholds);
  assert.strictEqual(r99.passedBreak, true);
  assert.strictEqual(r99.status, '🟡 LOW');

  const r96 = evaluateScore(96, thresholds);
  assert.strictEqual(r96.passedBreak, true);
  assert.strictEqual(r96.status, '🟠 WARNING');

  const r94 = evaluateScore(94.99, thresholds);
  assert.strictEqual(r94.passedBreak, false);
  assert.strictEqual(r94.status, '❌ FAILED');

  console.log('✅ Test 3 Passed: evaluateScore correctly maps all 4 threshold tiers');
}

// Test 4: verifyMutationGate with valid commit status
(async () => {
  const freshDate = new Date().toISOString();
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-concurrency' },
    sha: 'sha1234567890'
  };

  const outputs = {};
  const mockCore = {
    setOutput: (k, v) => { outputs[k] = v; },
    summary: {
      addRaw: () => ({
        write: async () => {}
      })
    }
  };

  const mockGithub = {
    rest: {
      repos: {
        listCommits: async () => ({
          data: [{ sha: 'sha1234567890', commit: { committer: { date: freshDate } } }]
        }),
        getCombinedStatusForRef: async () => ({
          data: {
            statuses: [
              {
                context: 'mutation-testing/stryker',
                state: 'success',
                description: 'Score: 100% (13/13 packages >= 95%) - ✅ HIGH',
                updated_at: freshDate,
                target_url: 'https://github.com/ericksonlopezf/dotnet-concurrency/actions/runs/123'
              }
            ]
          }
        })
      }
    }
  };

  const result = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(result.needsStryker, false);
  assert.strictEqual(result.canProceed, true);
  assert.strictEqual(outputs['needs_stryker'], 'false');
  assert.strictEqual(outputs['can_proceed'], 'true');
  console.log('✅ Test 4 Passed: verifyMutationGate allows release when fresh passing status exists');
})();

// Test 5: verifyMutationGate triggers when report is expired (> 7 days)
(async () => {
  const expiredDate = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000).toISOString();
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-concurrency' },
    sha: 'shaExpired'
  };

  const outputs = {};
  const mockCore = {
    setOutput: (k, v) => { outputs[k] = v; },
    summary: {
      addRaw: () => ({
        write: async () => {}
      })
    }
  };

  const mockGithub = {
    rest: {
      repos: {
        listCommits: async () => ({
          data: [{ sha: 'shaExpired', commit: { committer: { date: expiredDate } } }]
        }),
        getCombinedStatusForRef: async () => ({
          data: {
            statuses: [
              {
                context: 'mutation-testing/stryker',
                state: 'success',
                description: 'Score: 100% - ✅ HIGH',
                updated_at: expiredDate
              }
            ]
          }
        })
      }
    }
  };

  const result = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(result.needsStryker, true);
  assert.strictEqual(result.canProceed, false);
  assert.strictEqual(outputs['needs_stryker'], 'true');
  console.log('✅ Test 5 Passed: verifyMutationGate triggers Stryker when report exceeds 7 days TTL');
})();

// Test 6: verifyMutationGate triggers when production code drift occurs in src/
(async () => {
  const freshDate = new Date().toISOString();
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-concurrency' },
    sha: 'targetShaDrift'
  };

  const outputs = {};
  const mockCore = {
    setOutput: (k, v) => { outputs[k] = v; },
    summary: {
      addRaw: () => ({
        write: async () => {}
      })
    }
  };

  const mockGithub = {
    rest: {
      repos: {
        listCommits: async () => ({
          data: [
            { sha: 'targetShaDrift' },
            { sha: 'evaluatedShaEarlier', commit: { committer: { date: freshDate } } }
          ]
        }),
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'evaluatedShaEarlier') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Score: 100% - ✅ HIGH',
                    updated_at: freshDate
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        },
        compareCommits: async () => ({
          data: {
            files: [
              { filename: 'src/EricksonLopez.Concurrency/ConcurrencyController.cs' },
              { filename: 'README.md' }
            ]
          }
        })
      }
    }
  };

  const result = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(result.needsStryker, true);
  assert.strictEqual(result.canProceed, false);
  assert.strictEqual(outputs['needs_stryker'], 'true');
  console.log('✅ Test 6 Passed: verifyMutationGate triggers Stryker when src/ code drift is detected');
})();
