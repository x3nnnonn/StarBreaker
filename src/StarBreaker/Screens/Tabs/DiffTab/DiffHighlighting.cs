using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarBreaker.Screens;

public enum DiffLineType
{
    Unchanged,
    Added,
    Removed,
    Modified
}

public class DiffLine
{
    public string Content { get; set; } = "";
    public DiffLineType Type { get; set; }
    public int LineNumber { get; set; }
}

public class DiffHunk
{
    public int OldStartLine { get; set; }
    public int OldLineCount { get; set; }
    public int NewStartLine { get; set; }
    public int NewLineCount { get; set; }
    public List<DiffLine> Lines { get; set; } = new();
    public string Header => $"@@ -{OldStartLine},{OldLineCount} +{NewStartLine},{NewLineCount} @@";
}

public class DiffResult
{
    public List<DiffLine> OldLines { get; set; } = new();
    public List<DiffLine> NewLines { get; set; } = new();
    public List<DiffHunk> Hunks { get; set; } = new();
}

public static class DiffAlgorithm
{
    public static DiffResult Compare(string oldText, string newText)
    {
        var oldLines = oldText.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var newLines = newText.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        
        // Increase threshold since memory is usually not an issue for most XML files
        const int maxLinesForLCS = 50000; // Increased limit
        
        if (oldLines.Length > maxLinesForLCS || newLines.Length > maxLinesForLCS)
        {
            return SmartBlockDiff(oldLines, newLines);
        }
        
        try
        {
            return LcsDiff(oldLines, newLines);
        }
        catch (OutOfMemoryException)
        {
            // Fallback to smart block diff if LCS runs out of memory
            return SmartBlockDiff(oldLines, newLines);
        }
        catch (OverflowException)
        {
            // Fallback to smart block diff if array dimensions exceed limits
            return SmartBlockDiff(oldLines, newLines);
        }
    }
    
    private static DiffResult SmartBlockDiff(string[] oldLines, string[] newLines)
    {
        var result = new DiffResult();
        
        // Build a hash set of all lines in the new file for quick lookup
        var newLinesSet = new HashSet<string>(newLines);
        var oldLinesSet = new HashSet<string>(oldLines);
        
        // Process old lines
        for (int i = 0; i < oldLines.Length; i++)
        {
            var line = oldLines[i];
            DiffLineType type;
            
            if (newLinesSet.Contains(line))
            {
                // Line exists in new file - mark as unchanged
                type = DiffLineType.Unchanged;
            }
            else
            {
                // Line doesn't exist in new file - mark as removed
                type = DiffLineType.Removed;
            }
            
            result.OldLines.Add(new DiffLine
            {
                Content = line,
                Type = type,
                LineNumber = i + 1
            });
        }
        
        // Process new lines
        for (int i = 0; i < newLines.Length; i++)
        {
            var line = newLines[i];
            DiffLineType type;
            
            if (oldLinesSet.Contains(line))
            {
                // Line exists in old file - mark as unchanged
                type = DiffLineType.Unchanged;
            }
            else
            {
                // Line doesn't exist in old file - mark as added
                type = DiffLineType.Added;
            }
            
            result.NewLines.Add(new DiffLine
            {
                Content = line,
                Type = type,
                LineNumber = i + 1
            });
        }
        
        // Generate hunks for smart block diff as well
        result.Hunks = GenerateHunks(result.OldLines, result.NewLines);
        
        return result;
    }
    
    private static DiffResult LcsDiff(string[] oldLines, string[] newLines)
    {
        var result = new DiffResult();
        
        // LCS-based diff for smaller files
        var lcs = ComputeLCS(oldLines, newLines);
        
        int oldIndex = 0, newIndex = 0, lcsIndex = 0;
        
        while (oldIndex < oldLines.Length || newIndex < newLines.Length)
        {
            if (lcsIndex < lcs.Count && 
                oldIndex < oldLines.Length && 
                newIndex < newLines.Length &&
                oldLines[oldIndex] == lcs[lcsIndex] && 
                newLines[newIndex] == lcs[lcsIndex])
            {
                // Unchanged line
                result.OldLines.Add(new DiffLine 
                { 
                    Content = oldLines[oldIndex], 
                    Type = DiffLineType.Unchanged, 
                    LineNumber = oldIndex + 1 
                });
                result.NewLines.Add(new DiffLine 
                { 
                    Content = newLines[newIndex], 
                    Type = DiffLineType.Unchanged, 
                    LineNumber = newIndex + 1 
                });
                oldIndex++;
                newIndex++;
                lcsIndex++;
            }
            else if (oldIndex < oldLines.Length && 
                     (lcsIndex >= lcs.Count || oldLines[oldIndex] != lcs[lcsIndex]))
            {
                // Removed line
                result.OldLines.Add(new DiffLine 
                { 
                    Content = oldLines[oldIndex], 
                    Type = DiffLineType.Removed, 
                    LineNumber = oldIndex + 1 
                });
                result.NewLines.Add(new DiffLine 
                { 
                    Content = "", 
                    Type = DiffLineType.Removed, 
                    LineNumber = -1 
                });
                oldIndex++;
            }
            else if (newIndex < newLines.Length)
            {
                // Added line
                result.OldLines.Add(new DiffLine 
                { 
                    Content = "", 
                    Type = DiffLineType.Added, 
                    LineNumber = -1 
                });
                result.NewLines.Add(new DiffLine 
                { 
                    Content = newLines[newIndex], 
                    Type = DiffLineType.Added, 
                    LineNumber = newIndex + 1 
                });
                newIndex++;
            }
        }
        
        // Generate hunks from the diff result for both algorithms
        result.Hunks = GenerateHunks(result.OldLines, result.NewLines);
        
        return result;
    }
    
    private static List<DiffHunk> GenerateHunks(List<DiffLine> oldLines, List<DiffLine> newLines, int contextLines = 3)
    {
        var hunks = new List<DiffHunk>();
        
        // Find all changed line indices in the unified view
        var changedIndices = new HashSet<int>();
        
        for (int i = 0; i < Math.Max(oldLines.Count, newLines.Count); i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;
            
            if (oldLine?.Type != DiffLineType.Unchanged || newLine?.Type != DiffLineType.Unchanged)
            {
                changedIndices.Add(i);
            }
        }
        
        if (changedIndices.Count == 0) return hunks;
        
        // Group changes into hunks with context
        var sortedChanges = changedIndices.OrderBy(x => x).ToList();
        var currentHunkStart = -1;
        var currentHunkEnd = -1;
        
        foreach (var changeIndex in sortedChanges)
        {
            var hunkStart = Math.Max(0, changeIndex - contextLines);
            var hunkEnd = Math.Min(Math.Max(oldLines.Count, newLines.Count) - 1, changeIndex + contextLines);
            
            if (currentHunkStart == -1)
            {
                // Start new hunk
                currentHunkStart = hunkStart;
                currentHunkEnd = hunkEnd;
            }
            else if (hunkStart <= currentHunkEnd + contextLines * 2 + 1)
            {
                // Extend current hunk
                currentHunkEnd = Math.Max(currentHunkEnd, hunkEnd);
            }
            else
            {
                // Create hunk and start a new one
                hunks.Add(CreateHunk(oldLines, newLines, currentHunkStart, currentHunkEnd));
                currentHunkStart = hunkStart;
                currentHunkEnd = hunkEnd;
            }
        }
        
        // Add the final hunk
        if (currentHunkStart != -1)
        {
            hunks.Add(CreateHunk(oldLines, newLines, currentHunkStart, currentHunkEnd));
        }
        
        return hunks;
    }
    
    private static DiffHunk CreateHunk(List<DiffLine> oldLines, List<DiffLine> newLines, int startIndex, int endIndex)
    {
        var hunk = new DiffHunk();
        var hunkLines = new List<DiffLine>();
        
        var oldStartLine = -1;
        var newStartLine = -1;
        var oldCount = 0;
        var newCount = 0;
        
        for (int i = startIndex; i <= endIndex; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;
            
            // Create unified diff line
            DiffLine unifiedLine;
            
            if (oldLine?.Type == DiffLineType.Removed)
            {
                unifiedLine = new DiffLine
                {
                    Content = oldLine.Content,
                    Type = DiffLineType.Removed,
                    LineNumber = oldLine.LineNumber
                };
                if (oldStartLine == -1) oldStartLine = oldLine.LineNumber;
                oldCount++;
            }
            else if (newLine?.Type == DiffLineType.Added)
            {
                unifiedLine = new DiffLine
                {
                    Content = newLine.Content,
                    Type = DiffLineType.Added,
                    LineNumber = newLine.LineNumber
                };
                if (newStartLine == -1) newStartLine = newLine.LineNumber;
                newCount++;
            }
            else if (oldLine?.Type == DiffLineType.Unchanged && newLine?.Type == DiffLineType.Unchanged)
            {
                unifiedLine = new DiffLine
                {
                    Content = oldLine.Content,
                    Type = DiffLineType.Unchanged,
                    LineNumber = oldLine.LineNumber
                };
                if (oldStartLine == -1) oldStartLine = oldLine.LineNumber;
                if (newStartLine == -1) newStartLine = newLine.LineNumber;
                oldCount++;
                newCount++;
            }
            else
            {
                continue; // Skip empty entries
            }
            
            hunkLines.Add(unifiedLine);
        }
        
        hunk.Lines = hunkLines;
        hunk.OldStartLine = oldStartLine > 0 ? oldStartLine : 1;
        hunk.NewStartLine = newStartLine > 0 ? newStartLine : 1;
        hunk.OldLineCount = oldCount;
        hunk.NewLineCount = newCount;
        
        return hunk;
    }
    
    private static List<string> ComputeLCS(string[] oldLines, string[] newLines)
    {
        int[,] lengths = new int[oldLines.Length + 1, newLines.Length + 1];
        
        // Build the LCS matrix
        for (int i = 0; i <= oldLines.Length; i++)
        {
            for (int j = 0; j <= newLines.Length; j++)
            {
                if (i == 0 || j == 0)
                    lengths[i, j] = 0;
                else if (oldLines[i - 1] == newLines[j - 1])
                    lengths[i, j] = lengths[i - 1, j - 1] + 1;
                else
                    lengths[i, j] = Math.Max(lengths[i - 1, j], lengths[i, j - 1]);
            }
        }
        
        // Backtrack to find the LCS
        var lcs = new List<string>();
        int x = oldLines.Length, y = newLines.Length;
        
        while (x > 0 && y > 0)
        {
            if (oldLines[x - 1] == newLines[y - 1])
            {
                lcs.Insert(0, oldLines[x - 1]);
                x--;
                y--;
            }
            else if (lengths[x - 1, y] > lengths[x, y - 1])
                x--;
            else
                y--;
        }
        
        return lcs;
    }
}

public class DiffBackgroundRenderer : IBackgroundRenderer
{
    private readonly List<DiffLine> _diffLines;
    private readonly TextDocument _document;
    
    private static readonly SolidColorBrush AddedBrush = new(Color.FromArgb(120, 0, 200, 0));      // More prominent green
    private static readonly SolidColorBrush RemovedBrush = new(Color.FromArgb(120, 200, 0, 0));    // More prominent red
    private static readonly SolidColorBrush ModifiedBrush = new(Color.FromArgb(120, 200, 200, 0)); // More prominent yellow

    public DiffBackgroundRenderer(List<DiffLine> diffLines, TextDocument document)
    {
        _diffLines = diffLines;
        _document = document;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document != _document) return;
        
        foreach (var line in _diffLines.Where(l => l.Type != DiffLineType.Unchanged))
        {
            if (line.LineNumber <= 0 || line.LineNumber > _document.LineCount) continue;
            
            try
            {
                var documentLine = _document.GetLineByNumber(line.LineNumber);
                var segment = new AvaloniaEdit.Document.TextSegment
                {
                    StartOffset = documentLine.Offset,
                    Length = documentLine.Length
                };

                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                {
                    var brush = line.Type switch
                    {
                        DiffLineType.Added => AddedBrush,
                        DiffLineType.Removed => RemovedBrush,
                        DiffLineType.Modified => ModifiedBrush,
                        _ => null
                    };
                    
                    if (brush != null)
                    {
                        // Extend the rectangle to cover the full width
                        var fullWidthRect = new Rect(0, rect.Y, Math.Max(rect.Width, 500), rect.Height);
                        drawingContext.FillRectangle(brush, fullWidthRect);
                    }
                }
            }
            catch
            {
                // Ignore rendering errors for invalid lines
            }
        }
    }
} 