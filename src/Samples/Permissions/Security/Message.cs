﻿using System;

using Lokad.Cqrs.Extensions.Permissions;

namespace Security
{
    public class Message : ISecurableEntity
    {
        #region Implementation of ISecurableEntity

        public Guid SecurityKey { get; set; }

        #endregion
    }
}