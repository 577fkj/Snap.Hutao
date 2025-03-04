﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace Snap.Hutao.Web.Hoyolab.Takumi.Event.BbsSignReward;

/// <summary>
/// 签到提交数据
/// </summary>
public class SignInData
{
    /// <summary>
    /// 构造一个新的签到提交数据
    /// </summary>
    /// <param name="uid">uid</param>
    public SignInData(PlayerUid uid)
    {
        Region = uid.Region;
        Uid = uid.Value;
    }

    /// <summary>
    /// 活动Id
    /// </summary>
    [JsonPropertyName("act_id")]
    public string ActivityId { get; } = ApiEndpoints.SignInRewardActivityId;

    /// <summary>
    /// 地区代码
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; }

    /// <summary>
    /// Uid
    /// </summary>
    [JsonPropertyName("uid")]
    public string Uid { get; }
}
