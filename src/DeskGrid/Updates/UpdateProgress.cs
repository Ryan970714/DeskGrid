namespace DeskGrid.Updates;

internal enum UpdateStage
{
    Downloading,
    Verifying,
    Installing,
}

/// <summary>Where an update is: the stage, and for a download how many bytes of the total have arrived.</summary>
internal readonly record struct UpdateProgress(UpdateStage Stage, long Done = 0, long Total = 0);
