// <copyright file="SpoeAction.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="SpoeAction.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
namespace HAProxy.StreamProcessingOffload.Agent
{
    internal abstract class SpoeAction
    {
        protected SpoeAction(ActionType type)
        {
            this.Type = type;
        }

        public abstract byte[] Bytes { get; }

        public ActionType Type { get; private set; }
    }
}
