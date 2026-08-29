// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyProblemDetailsTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeRfc7807Defaults()
    {
        var details = new ConcurrencyProblemDetails();

        details.Status.Should().Be(StatusCodes.Status409Conflict);
        details.Title.Should().Be("Optimistic Concurrency Conflict");
        details.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.8");
        details.ConflictType.Should().BeNull();
        details.Classification.Should().BeNull();
        details.EntityId.Should().BeNull();
        details.EntityType.Should().BeNull();
        details.ExpectedVersion.Should().BeNull();
        details.ActualVersion.Should().BeNull();
    }

    [Fact]
    public void PropertySettersAndGetters_ShouldAssignAndRetrieveValues()
    {
        var details = new ConcurrencyProblemDetails
        {
            ConflictType = "CustomConflict",
            Classification = "Terminal",
            EntityId = "ent-99",
            EntityType = "Order",
            ExpectedVersion = "10",
            ActualVersion = "11"
        };

        details.ConflictType.Should().Be("CustomConflict");
        details.Classification.Should().Be("Terminal");
        details.EntityId.Should().Be("ent-99");
        details.EntityType.Should().Be("Order");
        details.ExpectedVersion.Should().Be("10");
        details.ActualVersion.Should().Be("11");
    }

    [Fact]
    public void From_NullConflict_ShouldThrowArgumentNullException()
    {
        Action act = () => ConcurrencyProblemDetails.From(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Fact]
    public void From_SpecificVersionsAndMetadata_ShouldPopulateDetails()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "prod-42",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "UpdateStock",
            message: "Version mismatch: expected 5, found 6.",
            expectedVersion: ExpectedVersion.Specific(5),
            actualVersion: ActualVersion.From(6),
            metadata: new Dictionary<string, string> { ["tenant"] = "t1", ["region"] = "us-east" });

        ConcurrencyProblemDetails details = ConcurrencyProblemDetails.From(conflict, "/api/products/prod-42");

        details.Status.Should().Be(StatusCodes.Status409Conflict);
        details.Title.Should().Be("Concurrency Conflict: VersionMismatch");
        details.Detail.Should().Be("Version mismatch: expected 5, found 6.");
        details.Instance.Should().Be("/api/products/prod-42");
        details.ConflictType.Should().Be("VersionMismatch");
        details.Classification.Should().Be("Transient");
        details.EntityId.Should().Be("prod-42");
        details.EntityType.Should().Be("Product");
        details.ExpectedVersion.Should().Be("5");
        details.ActualVersion.Should().Be("6");
        details.Extensions.Should().ContainKey("tenant").WhoseValue!.ToString().Should().Be("t1");
        details.Extensions.Should().ContainKey("region").WhoseValue!.ToString().Should().Be("us-east");
    }

    [Fact]
    public void From_ExpectedVersionKindsAndActualVersionStates_ShouldFormatCorrectly()
    {
        // ExpectedVersion.Any and ActualVersion.NotFound
        var conflictAny = new ConcurrencyConflict(
            entityId: "e1",
            entityType: "Type1",
            conflictType: ConcurrencyConflictType.Custom,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: "Op1",
            message: "Msg",
            expectedVersion: ExpectedVersion.Any,
            actualVersion: ActualVersion.NotFound);

        ConcurrencyProblemDetails detailsAny = ConcurrencyProblemDetails.From(conflictAny);
        detailsAny.ExpectedVersion.Should().Be("Any");
        detailsAny.ActualVersion.Should().Be("NotFound");
        detailsAny.Instance.Should().BeNull();
        detailsAny.Extensions.Should().BeEmpty();

        // ExpectedVersion.New and ActualVersion null
        var conflictNew = new ConcurrencyConflict(
            entityId: "e2",
            entityType: "Type2",
            conflictType: ConcurrencyConflictType.Custom,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: "Op2",
            message: "Msg",
            expectedVersion: ExpectedVersion.New,
            actualVersion: null);

        ConcurrencyProblemDetails detailsNew = ConcurrencyProblemDetails.From(conflictNew);
        detailsNew.ExpectedVersion.Should().Be("New");
        detailsNew.ActualVersion.Should().BeNull();

        // ExpectedVersion.Exists
        var conflictExists = new ConcurrencyConflict(
            entityId: "e3",
            entityType: "Type3",
            conflictType: ConcurrencyConflictType.Custom,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: "Op3",
            message: "Msg",
            expectedVersion: ExpectedVersion.Exists,
            actualVersion: null);

        ConcurrencyProblemDetails detailsExists = ConcurrencyProblemDetails.From(conflictExists);
        detailsExists.ExpectedVersion.Should().Be("Exists");

        // ExpectedVersion null
        var conflictNoVersions = new ConcurrencyConflict(
            entityId: "e4",
            entityType: "Type4",
            conflictType: ConcurrencyConflictType.Custom,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: "Op4",
            message: "Msg",
            expectedVersion: null,
            actualVersion: null);

        ConcurrencyProblemDetails detailsNoVersions = ConcurrencyProblemDetails.From(conflictNoVersions);
        detailsNoVersions.ExpectedVersion.Should().BeNull();
        detailsNoVersions.ActualVersion.Should().BeNull();
    }
}
