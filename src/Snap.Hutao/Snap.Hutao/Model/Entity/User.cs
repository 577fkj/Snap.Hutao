﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Model.Entity;

/// <summary>
/// 用户
/// </summary>
[Table("users")]
public class User : ISelectable
{
    /// <summary>
    /// 内部Id
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    /// <summary>
    /// 是否被选中
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// 用户的Cookie
    /// </summary>
    public string? Cookie { get; set; }

    /// <summary>
    /// 创建一个新的用户
    /// </summary>
    /// <param name="cookie">cookie</param>
    /// <returns>新创建的用户</returns>
    public static User Create(string cookie)
    {
        return new() { Cookie = cookie };
    }
}
