namespace Kommand.Tests.Unit;

using Kommand;

/// <summary>
/// Unit tests for the Unit type.
/// </summary>
public class UnitTypeTests
{
    /// <summary>
    /// Verifies that Unit.Value returns the same singleton instance.
    /// </summary>
    [Fact]
    public void UnitValue_ShouldReturnSameInstance()
    {
        // Act
        var value1 = Unit.Value;
        var value2 = Unit.Value;

        // Assert
        Assert.Equal(value1, value2);
    }

    /// <summary>
    /// Verifies that Unit instances are equal using the Equals method.
    /// </summary>
    [Fact]
    public void Unit_ShouldBeEqualToOtherUnitInstances()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = default(Unit);
        var unit3 = new Unit();

        // Assert
        Assert.Equal(unit1, unit2);
        Assert.Equal(unit1, unit3);
        Assert.Equal(unit2, unit3);
    }

    /// <summary>
    /// Verifies that Unit instances have the same hash code.
    /// </summary>
    [Fact]
    public void Unit_ShouldHaveSameHashCode()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = default(Unit);

        // Act
        var hash1 = unit1.GetHashCode();
        var hash2 = unit2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    /// Verifies that Unit.Value can be returned from async methods.
    /// </summary>
    [Fact]
    public async Task UnitValue_CanBeReturnedFromAsyncMethods()
    {
        // Act
        var result = await ReturnsUnitAsync();

        // Assert
        Assert.Equal(Unit.Value, result);
    }

    /// <summary>
    /// Verifies that Unit can be used in Task.FromResult.
    /// </summary>
    [Fact]
    public async Task Unit_CanBeUsedWithTaskFromResult()
    {
        // Act
        var result = await Task.FromResult(Unit.Value);

        // Assert
        Assert.Equal(Unit.Value, result);
    }

    private static async Task<Unit> ReturnsUnitAsync()
    {
        await Task.Delay(1);
        return Unit.Value;
    }
}
