using Chainer.Configuration;
using Chainer.Configuration.Binding;
using Chainer.Utilities.Hashing;
using System.Text.Json;

namespace Chainer.Tests.Configuration.Binding;

public class ChainConfigurationGroupTest
{
    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_ShouldInitializePropertiesWithDefaults()
    {
        // Act
        var group = new ChainConfigurationGroup();

        // Assert
        group.ContextTypeName.ShouldBe(string.Empty);
        group.Chains.ShouldNotBeNull();
        group.Chains.Count.ShouldBe(0);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var group = new ChainConfigurationGroup();
        var contextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!;
        var chains = new List<ChainConfigurationItem>
        {
            new() { HandlerTypeName = "Handler1" },
            new() { HandlerTypeName = "Handler2" }
        };

        // Act
        group.ContextTypeName = contextTypeName;
        group.Chains = chains;

        // Assert
        group.ContextTypeName.ShouldBe(contextTypeName);
        group.Chains.ShouldBe(chains);
        group.Chains.Count.ShouldBe(2);
    }

    #endregion

    #region ToChainMessages Tests

    [Fact]
    public void ToChainMessages_WithEmptyChains_ShouldReturnEmptyList()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = []
        };

        // Act
        var messages = group.ToChainMessages("TestChain");

        // Assert
        messages.ShouldNotBeNull();
        messages.Count.ShouldBe(0);
    }

    [Fact]
    public void ToChainMessages_WithSingleHandler_ShouldCreateCorrectMessage()
    {
        // Arrange
        var handlerTypeName = typeof(IncrementHandler).AssemblyQualifiedName!;
        var contextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!;
        const string chainName = "SingleHandlerChain";

        var group = new ChainConfigurationGroup
        {
            ContextTypeName = contextTypeName,
            Chains = [new ChainConfigurationItem { HandlerTypeName = handlerTypeName }]
        };

        // Act
        var messages = group.ToChainMessages(chainName);

        // Assert
        messages.Count.ShouldBe(1);
        var message = messages[0];
        message.HandlerTypeName.ShouldBe(handlerTypeName);
        message.ContextTypeName.ShouldBe(contextTypeName);
        message.ExecutionOrder.ShouldBe(0);
        message.FriendlyName.ShouldBe(chainName);
        message.ConfigurationType.ShouldBe(HandlerConfigurationType.Json);
        message.Configuration.ShouldBeNull();
        message.Id.ShouldNotBe(Guid.Empty);
        message.ChainId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void ToChainMessages_WithMultipleHandlers_ShouldAssignCorrectExecutionOrder()
    {
        // Arrange
        var handlers = new[] { "Handler1", "Handler2", "Handler3", "Handler4" };
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = handlers.Select(h => new ChainConfigurationItem { HandlerTypeName = h }).ToList()
        };

        // Act
        var messages = group.ToChainMessages("OrderedChain");

        // Assert
        messages.Count.ShouldBe(4);
        for (var i = 0; i < messages.Count; i++)
        {
            messages[i].HandlerTypeName.ShouldBe(handlers[i]);
            messages[i].ExecutionOrder.ShouldBe(i);
        }
    }

    [Fact]
    public void ToChainMessages_WithConfiguration_ShouldSerializeToJson()
    {
        // Arrange
        var config = new { IncrementBy = 5, MaxValue = 100 };
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem
                {
                    HandlerTypeName = "ConfigurableHandler",
                    Configuration = config
                }
            ]
        };

        // Act
        var messages = group.ToChainMessages("ConfiguredChain");

        // Assert
        messages.Count.ShouldBe(1);
        var message = messages[0];
        message.Configuration.ShouldNotBeNull();

        // Verify JSON serialization
        var deserializedConfig = JsonSerializer.Deserialize<Dictionary<string, int>>(message.Configuration);
        deserializedConfig.ShouldNotBeNull();
        deserializedConfig["IncrementBy"].ShouldBe(5);
        deserializedConfig["MaxValue"].ShouldBe(100);
    }

    [Fact]
    public void ToChainMessages_ShouldGenerateDeterministicChainId()
    {
        // Arrange
        const string chainName = "DeterministicChain";
        var expectedChainId = GuidFromString.CreateDeterministicGuid(chainName);

        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem { HandlerTypeName = "Handler1" },
                new ChainConfigurationItem { HandlerTypeName = "Handler2" }
            ]
        };

        // Act
        var messages = group.ToChainMessages(chainName);

        // Assert
        messages.All(m => m.ChainId == expectedChainId).ShouldBeTrue();

        // Verify deterministic behavior
        var messages2 = group.ToChainMessages(chainName);
        messages[0].ChainId.ShouldBe(messages2[0].ChainId);
    }

    [Fact]
    public void ToChainMessages_WithDifferentNames_ShouldGenerateDifferentChainIds()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = [new ChainConfigurationItem { HandlerTypeName = "Handler1" }]
        };

        // Act
        var messages1 = group.ToChainMessages("Chain1");
        var messages2 = group.ToChainMessages("Chain2");

        // Assert
        messages1[0].ChainId.ShouldNotBe(messages2[0].ChainId);
    }

    [Fact]
    public void ToChainMessages_ShouldGenerateUniqueMessageIds()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem { HandlerTypeName = "Handler1" },
                new ChainConfigurationItem { HandlerTypeName = "Handler2" },
                new ChainConfigurationItem { HandlerTypeName = "Handler3" }
            ]
        };

        // Act
        var messages = group.ToChainMessages("UniqueIdChain");

        // Assert
        var messageIds = messages.Select(m => m.Id).ToList();
        messageIds.Distinct().Count().ShouldBe(3);
        messageIds.All(id => id != Guid.Empty).ShouldBeTrue();
    }

    [Fact]
    public void ToChainMessages_WithCustomConfigurationType_ShouldUseSpecifiedType()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem
                {
                    HandlerTypeName = "XmlConfigHandler",
                    Configuration = new { Value = "Test" }
                }
            ]
        };

        // Act
        var messages = group.ToChainMessages("XmlChain", HandlerConfigurationType.Xml);

        // Assert
        messages[0].ConfigurationType.ShouldBe(HandlerConfigurationType.Xml);
    }

    [Fact]
    public void ToChainMessages_AllMessages_ShouldShareSameFriendlyName()
    {
        // Arrange
        const string friendlyName = "SharedNameChain";
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem { HandlerTypeName = "Handler1" },
                new ChainConfigurationItem { HandlerTypeName = "Handler2" },
                new ChainConfigurationItem { HandlerTypeName = "Handler3" }
            ]
        };

        // Act
        var messages = group.ToChainMessages(friendlyName);

        // Assert
        messages.All(m => m.FriendlyName == friendlyName).ShouldBeTrue();
    }

    [Fact]
    public void ToChainMessages_WithComplexConfiguration_ShouldSerializeCorrectly()
    {
        // Arrange
        var complexConfig = new
        {
            StringValue = "Test",
            IntValue = 42,
            BoolValue = true,
            DecimalValue = 123.45m,
            DateValue = new DateTime(2023, 1, 1),
            NestedObject = new
            {
                Name = "Nested",
                Value = 100
            },
            ListValue = new[] { 1, 2, 3 }
        };

        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem
                {
                    HandlerTypeName = "ComplexConfigHandler",
                    Configuration = complexConfig
                }
            ]
        };

        // Act
        var messages = group.ToChainMessages("ComplexConfigChain");

        // Assert
        var message = messages[0];
        message.Configuration.ShouldNotBeNull();

        // Verify the configuration can be deserialized back
        using var doc = JsonDocument.Parse(message.Configuration);
        var root = doc.RootElement;
        root.GetProperty("StringValue").GetString().ShouldBe("Test");
        root.GetProperty("IntValue").GetInt32().ShouldBe(42);
        root.GetProperty("BoolValue").GetBoolean().ShouldBeTrue();
        root.GetProperty("NestedObject").GetProperty("Name").GetString().ShouldBe("Nested");
        root.GetProperty("ListValue").GetArrayLength().ShouldBe(3);
    }

    [Fact]
    public void ToChainMessages_WithNullConfiguration_ShouldHaveNullConfigurationString()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains =
            [
                new ChainConfigurationItem
                {
                    HandlerTypeName = "NoConfigHandler",
                    Configuration = null
                }
            ]
        };

        // Act
        var messages = group.ToChainMessages("NullConfigChain");

        // Assert
        messages[0].Configuration.ShouldBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ToChainMessages_WithEmptyChainName_ShouldStillGenerateValidChainId()
    {
        // Arrange
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = [new ChainConfigurationItem { HandlerTypeName = "Handler1" }]
        };

        // Act
        var messages = group.ToChainMessages("");

        // Assert
        messages[0].ChainId.ShouldNotBe(Guid.Empty);
        messages[0].FriendlyName.ShouldBe("");
    }

    [Fact]
    public void ToChainMessages_WithSpecialCharactersInName_ShouldWork()
    {
        // Arrange
        const string specialName = "Chain-With_Special.Characters!@#$%^&*()";
        var group = new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = [new ChainConfigurationItem { HandlerTypeName = "Handler1" }]
        };

        // Act
        var messages = group.ToChainMessages(specialName);

        // Assert
        messages[0].FriendlyName.ShouldBe(specialName);
        messages[0].ChainId.ShouldNotBe(Guid.Empty);
    }

    #endregion
}