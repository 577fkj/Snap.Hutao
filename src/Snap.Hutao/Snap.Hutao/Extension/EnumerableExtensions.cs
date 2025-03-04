﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Snap.Hutao.Extension;

/// <summary>
/// <see cref="IEnumerable{T}"/> 扩展
/// </summary>
public static partial class EnumerableExtensions
{
    /// <summary>
    /// 计数
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TKey">计数的键类型</typeparam>
    /// <param name="source">源</param>
    /// <param name="keySelector">键选择器</param>
    /// <returns>计数表</returns>
    public static IEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull, IEquatable<TKey>
    {
        CounterInt32<TKey> counter = new();
        foreach (TSource item in source)
        {
            counter.Increase(keySelector(item));
        }

        return counter;
    }

    /// <summary>
    /// 如果传入集合不为空则原路返回，
    /// 如果传入集合为空返回一个集合的空集
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <param name="source">源</param>
    /// <returns>源集合或空集</returns>
    public static IEnumerable<TSource> EmptyIfNull<TSource>(this IEnumerable<TSource>? source)
    {
        return source ?? Enumerable.Empty<TSource>();
    }

    /// <summary>
    /// 如果传入列表不为空则原路返回，
    /// 如果传入列表为空返回一个空的列表
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <param name="source">源</param>
    /// <returns>源列表或空列表</returns>
    public static List<TSource> EmptyIfNull<TSource>(this List<TSource>? source)
    {
        return source ?? new();
    }

    /// <summary>
    /// 将源转换为仅包含单个元素的枚举
    /// </summary>
    /// <typeparam name="TSource">源的类型</typeparam>
    /// <param name="source">源</param>
    /// <returns>集合</returns>
    public static IEnumerable<TSource> Enumerate<TSource>(this TSource source)
    {
        yield return source;
    }

    /// <summary>
    /// 寻找枚举中唯一的值,找不到时
    /// 回退到首个或默认值
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <param name="source">源</param>
    /// <param name="predicate">谓语</param>
    /// <returns>目标项</returns>
    public static TSource? FirstOrFirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.FirstOrDefault(predicate) ?? source.FirstOrDefault();
    }

    /// <summary>
    /// 表示一个对 <see cref="TItem"/> 类型的计数器
    /// </summary>
    /// <typeparam name="TItem">待计数的类型</typeparam>
    private class CounterInt32<TItem> : Dictionary<TItem, int>
        where TItem : notnull, IEquatable<TItem>
    {
        /// <summary>
        /// 增加计数器
        /// </summary>
        /// <param name="item">物品</param>
        public void Increase(TItem? item)
        {
            if (item != null)
            {
                if (!ContainsKey(item))
                {
                    this[item] = 0;
                }

                this[item] += 1;
            }
        }
    }
}