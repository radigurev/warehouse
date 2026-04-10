using Warehouse.Common.Enums;
using Warehouse.Infrastructure.Sequences;

namespace Warehouse.Infrastructure.Tests.Sequences;

/// <summary>
/// Tests for <see cref="SequenceDefinition"/> configuration validation
/// and the built-in definitions registry.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-003")]
public sealed class SequenceDefinitionTests
{
    [Test]
    public void SequenceDefinition_DuplicateKey_ThrowsInvalidOperationException()
    {
        // Arrange
        SequenceDefinition first = new()
        {
            SequenceKey = "DUP",
            Prefix = "DUP-",
            ResetPolicy = SequenceResetPolicy.Never,
            Padding = 4,
            IncludesDateSegment = false
        };

        SequenceDefinition duplicate = new()
        {
            SequenceKey = "DUP",
            Prefix = "DUP-",
            ResetPolicy = SequenceResetPolicy.Never,
            Padding = 4,
            IncludesDateSegment = false
        };

        Dictionary<string, SequenceDefinition> dictionary = new(StringComparer.OrdinalIgnoreCase);
        first.Validate();
        dictionary.Add(first.SequenceKey, first);

        // Act & Assert
        duplicate.Validate();
        Assert.That(
            () => dictionary.Add(duplicate.SequenceKey, duplicate),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SequenceDefinition_DailyResetWithoutDateSegment_ThrowsInvalidOperationException()
    {
        // Arrange
        SequenceDefinition definition = new()
        {
            SequenceKey = "TEST",
            Prefix = "TEST-",
            ResetPolicy = SequenceResetPolicy.Daily,
            Padding = 4,
            IncludesDateSegment = false,
            DateFormat = null
        };

        // Act & Assert
        Assert.That(
            () => definition.Validate(),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("IncludesDateSegment"));
    }

    [Test]
    public void SequenceDefinition_NeverResetWithDateSegment_ThrowsInvalidOperationException()
    {
        // Arrange
        SequenceDefinition definition = new()
        {
            SequenceKey = "TEST",
            Prefix = "TEST-",
            ResetPolicy = SequenceResetPolicy.Never,
            Padding = 4,
            IncludesDateSegment = true,
            DateFormat = "yyyyMMdd"
        };

        // Act & Assert
        Assert.That(
            () => definition.Validate(),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("Never"));
    }

    [Test]
    public void SequenceDefinition_ZeroPadding_ThrowsInvalidOperationException()
    {
        // Arrange
        SequenceDefinition definition = new()
        {
            SequenceKey = "TEST",
            Prefix = "TEST-",
            ResetPolicy = SequenceResetPolicy.Never,
            Padding = 0,
            IncludesDateSegment = false
        };

        // Act & Assert
        Assert.That(
            () => definition.Validate(),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("Padding"));
    }

    [Test]
    public void SequenceDefinition_AllBuiltInDefinitions_AreValid()
    {
        // Act
        IReadOnlyDictionary<string, SequenceDefinition> definitions =
            SequenceDefinitions.GetBuiltInDefinitions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(definitions, Has.Count.EqualTo(12));
            Assert.That(definitions.ContainsKey("PO"), Is.True);
            Assert.That(definitions.ContainsKey("GR"), Is.True);
            Assert.That(definitions.ContainsKey("SR"), Is.True);
            Assert.That(definitions.ContainsKey("SO"), Is.True);
            Assert.That(definitions.ContainsKey("PL"), Is.True);
            Assert.That(definitions.ContainsKey("PKG"), Is.True);
            Assert.That(definitions.ContainsKey("SHP"), Is.True);
            Assert.That(definitions.ContainsKey("CR"), Is.True);
            Assert.That(definitions.ContainsKey("SUPP"), Is.True);
            Assert.That(definitions.ContainsKey("PROD"), Is.True);
            Assert.That(definitions.ContainsKey("CUST"), Is.True);
            Assert.That(definitions.ContainsKey("BATCH"), Is.True);
        });
    }
}
