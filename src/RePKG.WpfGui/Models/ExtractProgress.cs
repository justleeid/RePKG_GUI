using System;
using System.Collections.Generic;

namespace RePKG.WpfGui.Models;

/// <summary>
/// 解包进度状态
/// </summary>
public enum ExtractItemStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,

    /// <summary>
    /// 解包中
    /// </summary>
    Extracting,

    /// <summary>
    /// TEX 转换中
    /// </summary>
    Converting,

    /// <summary>
    /// 完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 跳过（已存在）
    /// </summary>
    Skipped
}

/// <summary>
/// 解包进度信息
/// </summary>
public record ExtractProgress
{
    /// <summary>
    /// 总项目数
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// 已完成数
    /// </summary>
    public int CompletedItems { get; init; }

    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedItems { get; init; }

    /// <summary>
    /// 当前处理项标题
    /// </summary>
    public string? CurrentItemTitle { get; init; }

    /// <summary>
    /// 当前项状态
    /// </summary>
    public ExtractItemStatus Status { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }

    // ── 文件级进度 ──

    /// <summary>
    /// 当前 PKG 内总文件数
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// 当前 PKG 内已处理文件数
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// 当前正在处理的文件名
    /// </summary>
    public string? CurrentFileName { get; init; }

    /// <summary>
    /// 进度百分比（PKG 级）
    /// </summary>
    public double ProgressPercent => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;

    /// <summary>
    /// 当前 PKG 内文件进度百分比
    /// </summary>
    public double FileProgressPercent => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}

/// <summary>
/// 解包结果
/// </summary>
public record ExtractResult
{
    /// <summary>
    /// 成功数
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 跳过数
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<ExtractError> Errors { get; init; } = [];

    /// <summary>
    /// 耗时
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}

/// <summary>
/// 解包错误信息
/// </summary>
public record ExtractError
{
    /// <summary>
    /// PKG 路径
    /// </summary>
    public string PkgPath { get; init; } = "";

    /// <summary>
    /// 壁纸标题
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Message { get; init; } = "";
}
