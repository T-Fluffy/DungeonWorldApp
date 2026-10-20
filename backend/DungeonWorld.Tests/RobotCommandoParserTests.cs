using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF22 Robot Commando per-section fixes: sol21-adjudicated exit targets
/// (appended when absent) and render/sol-proven garble rewrites. Complements the refs
/// golden (which gates the full graph but skips when CleanedData is absent); these run
/// on pure strings with no book data.
/// </summary>
public sealed class RobotCommandoParserTests
{
    [Theory]
    [InlineData(5, "If you leave the road, turn to 935.", "turn to 235.")]
    [InlineData(155, "Otherwise, turn to 635,", "turn to 335,")]
    [InlineData(14, "Ask for military information?. ~~ Turn to 3¢", "Turn to 36")]
    [InlineData(14, "sleeping sickness? Turn to Be", "Turn to 80")]
    [InlineData(14, "on the Karosseans? Turn to sf", "Turn to 58")]
    [InlineData(102, "turn immediately to 25a.", "turn immediately to 250")]
    [InlineData(45, "If you run for cover, turn to 99.", "turn to 159.")]
    [InlineData(49, "The Temple of Glory? Turn to 330", "The Temple of Glory? Turn to 339")]
    [InlineData(53, "Return to 8s,", "turn to 85,")]
    [InlineData(17, "If this means nothing to you, retum to 8g.", "turn to 89.")]
    [InlineData(102, "turn immediately to 25a.", "turn immediately to 250")]
    [InlineData(165, "Visit the airfield? Turn to 42", "Visit the airfield? Turn to 41")]
    [InlineData(67, "If you fly away from them, turn toxz.", "turn to 12.")]
    [InlineData(87, "wearing an enemy uniform, turn toyz. if", "turn to 177. if")]
    [InlineData(177, "peacefully. Turn to \u00e95.", "Turn to 65.")]
    [InlineData(194, "Whether or not you take it, turn back to 308.", "turn to 308.")]
    [InlineData(199, "peacefully. Turn to fi.", "Turn to 65.")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, RobotCommandoParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// Every code edge is restored when absent. Covers the sol21 chain pairs whose
    /// "Return to" verb defeats the cleaner plus computed/conditional references.
    /// </summary>
    [Theory]
    [InlineData(392, 333)]
    [InlineData(79, 288)]
    [InlineData(41, 165)]
    [InlineData(168, 14)]
    [InlineData(352, 166)]
    [InlineData(107, 166)]
    [InlineData(17, 53)]
    [InlineData(359, 200)]
    [InlineData(103, 111)]
    [InlineData(102, 250)]
    [InlineData(200, 378)]
    [InlineData(206, 265)]
    [InlineData(148, 91)]
    [InlineData(178, 94)]
    public void CodeEdge_RestoredWhenAbsent(int section, int target)
    {
        var content = "Body text without exits.";
        var fixed_ = RobotCommandoParser.ApplySectionFixes(section, content);
        Assert.Contains($"(turn to {target}).", fixed_);
    }

    /// <summary>
    /// A code edge already present in the content is not appended a second time.
    /// Parents with exactly one code edge keep the assertion exact.
    /// </summary>
    [Theory]
    [InlineData(392, 333)]
    [InlineData(79, 288)]
    [InlineData(41, 165)]
    public void CodeEdge_NotDuplicatedWhenPresent(int section, int target)
    {
        var withExit = $"Body text (turn to {target}).";
        Assert.Equal(withExit, RobotCommandoParser.ApplySectionFixes(section, withExit));
    }

    [Fact]
    public void ApplySectionFixes_LeavesOrdinaryProseAlone()
    {
        const string content = "You enter the village and investigate. Turn to 361.";
        Assert.Equal(content, RobotCommandoParser.ApplySectionFixes(13, content));
    }
}
