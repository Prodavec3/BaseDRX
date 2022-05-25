using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow.Server
{
  partial class AccessRightsRuleFunctions
  {

    /// <summary>
    /// Запустить агент автоматической выдачи прав.
    /// </summary>
    [Remote]
    public static void EnqueueAccessRightsAgent()
    {
      Docflow.Jobs.GrantAccessRightsToDocuments.Enqueue();
    }
    
    /// <summary>
    /// Получить действующие правила назначения прав по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Правила назначения прав.</returns>
    [Public]
    public static IQueryable<IAccessRightsRule> GetAccessRightsRulesByDocumentKind(IDocumentKind documentKind)
    {
      var rules = AccessRightsRules.GetAll();
      return AccessRightsRules.GetAll(r => r.Status == Docflow.AccessRightsRule.Status.Active)
        .Where(r => r.DocumentKinds.Any(k => k.DocumentKind.Id == documentKind.Id));
    }
  }
}