﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar;

/// <summary>
/// 包装详细角色信息列表
/// </summary>
public class CharacterWrapper
{
    /// <summary>
    /// 角色列表
    /// </summary>
    [JsonPropertyName("avatars")]
    public List<Character> Avatars { get; set; } = default!;
}
