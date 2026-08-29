namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習詳細画面における、曲ごとの参加者内訳（○表示用）。</summary>
public class SongParticipation
{
    public required string PieceId { get; init; }
    public required string Title { get; init; }
    public required List<ParticipationDot> Dots { get; init; }

    /// <summary>この練習におけるこの曲の録音音源へのリンク（OneDriveなど）。未登録の場合は null。</summary>
    public string? RecordingUrl { get; init; }

    /// <summary>録音音源へのリンクが登録済みかどうか。</summary>
    public bool HasRecordingUrl => !string.IsNullOrEmpty(RecordingUrl);

    /// <summary>「音源」タブで強調表示（ピン留め）されているかどうか。</summary>
    public bool IsFeatured { get; init; }

    /// <summary>強調表示をオンにする操作を提示できるかどうか（録音登録済み・未強調の場合）。</summary>
    public bool CanToggleFeaturedOn => HasRecordingUrl && !IsFeatured;

    /// <summary>強調表示をオフにする操作を提示できるかどうか（録音登録済み・強調中の場合）。</summary>
    public bool CanToggleFeaturedOff => HasRecordingUrl && IsFeatured;
}

/// <summary>参加者1人分を表す○1個分の表示データ。</summary>
public class ParticipationDot
{
    /// <summary>参加者が所属するパートの色（参加人数カードの塗りつぶし色と同一）。</summary>
    public required string ColorHex { get; init; }

    /// <summary>曲内で上下分割されている場合の「上」／「下」表示。分割なし・未選択の場合は null。</summary>
    public string? SubLabel { get; init; }

    /// <summary>パートの区切りを示すための余白（パートが切り替わる先頭の○のみ広めに取る）。CSS margin 値。</summary>
    public string Margin { get; init; } = "4px";
}
