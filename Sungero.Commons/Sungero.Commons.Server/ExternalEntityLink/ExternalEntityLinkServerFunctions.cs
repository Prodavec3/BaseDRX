using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.ExternalEntityLink;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Commons.Server
{
  partial class ExternalEntityLinkFunctions
  {
    /// <summary>
    /// Получить сущность.
    /// </summary>
    /// <returns>Сущность.</returns>
    [Remote(IsPure = true)]
    public Sungero.Domain.Shared.IEntity GetEntity()
    {
      var entityType = new System.Guid(_obj.EntityType).GetTypeByGuid();
      if (_obj.EntityId.HasValue)
      {
        using (var session = new Sungero.Domain.Session())
        {
          return session.Get(entityType, _obj.EntityId.Value);
        }
      }
      return _obj;
    }
    
  }
}