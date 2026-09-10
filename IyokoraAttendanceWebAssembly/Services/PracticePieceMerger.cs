using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>練習予定の演奏予定曲編集画面における、選択状態から保存用の曲一覧を組み立てるロジック。</summary>
public static class PracticePieceMerger
{
    /// <summary>
    /// チェックボックスでの選択結果から、保存用の演奏予定曲一覧を組み立てる。
    /// 既存の演奏予定曲については録音リンク・強調表示の設定を維持し、新たに選択された曲は
    /// 録音リンク未登録・強調表示なしの状態で追加する。選択が外された曲は結果に含めない。
    /// </summary>
    /// <param name="existingPieces">この練習に現在登録されている演奏予定曲。</param>
    /// <param name="selections">編集画面でのチェックボックスの選択状態。</param>
    public static List<PracticePieceRef> MergeSelectedPieces(IReadOnlyList<PracticePieceRef> existingPieces, IReadOnlyList<PieceSelectionInput> selections)
    {
        var existingByPieceId = existingPieces.ToDictionary(p => p.PieceId);
        return selections
            .Where(s => s.IsSelected)
            .Select(s => existingByPieceId.TryGetValue(s.PieceId, out var existing)
                ? existing
                : new PracticePieceRef { PieceId = s.PieceId, Title = s.Title })
            .ToList();
    }
}
