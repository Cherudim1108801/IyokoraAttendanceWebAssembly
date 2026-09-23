namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習詳細画面における、曲ごとの参加者内訳（○表示用）。</summary>
public class SongParticipation
{
    public required string PieceId { get; init; }
    public required string Title { get; init; }
    public required List<ParticipationDot> Dots { get; init; }

    /// <summary>この練習におけるこの曲の録音音源一覧。1曲につき複数件登録できる。</summary>
    public required List<PracticeRecording> Recordings { get; init; }

    /// <summary>録音音源が1件以上登録済みかどうか。</summary>
    public bool HasRecordings => Recordings.Count > 0;
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
