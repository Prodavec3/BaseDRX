using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Server
{
  partial class DocumentKindFunctions
  {
    /// <summary>
    /// Получить список доступных видов документов для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Доступные виды.</returns>
    [Public]
    public static IQueryable<IDocumentKind> GetAvailableDocumentKinds(IOfficialDocument document)
    {
      var documentKinds = DocumentKinds.GetAll(r => r.Status == CoreEntities.DatabookEntry.Status.Active);
      
      var type = document.GetType();
      
      var typeProperty = type.GetProperty("TypeGuid");
      if (typeProperty != null)
      {
        var typeGuid = ((Guid)typeProperty.GetValue(document)).GetOriginalTypeGuid().ToString();
        documentKinds = documentKinds.Where(k => k.DocumentType.DocumentTypeGuid == typeGuid);
      }
      
      return documentKinds;
    }
    
    /// <summary>
    /// Получить вид, созданный при инициализации.
    /// </summary>
    /// <param name="externalLink">Уникальный GUID для конкретного вида.</param>
    /// <returns>Вид документа.</returns>
    [Public]
    public static IDocumentKind GetNativeDocumentKind(Guid externalLink)
    {
      var link = Functions.Module.GetExternalLink(DocumentKind.ClassTypeGuid, externalLink);
      if (link == null)
        return null;
      
      return DocumentKinds.GetAll(r => r.Id == link.EntityId).SingleOrDefault();
    }
    
    /// <summary>
    /// Получить вид, созданный при инициализации.
    /// </summary>
    /// <param name="externalLink">Уникальный GUID для конкретного вида.</param>
    /// <returns>Вид документа.</returns>
    /// <remarks>Используется при импорте шаблонов документов.</remarks>
    [Public, Remote(IsPure = true)]
    public static IDocumentKind GetNativeDocumentKindRemote(Guid externalLink)
    {
      return GetNativeDocumentKind(externalLink);
    }
    
    /// <summary>
    /// Получить Guid вида документа.
    /// </summary>
    /// <returns>Guid вида документа.</returns>
    public virtual string GetDocumentKindGuid()
    {
      return Domain.ModuleFunctions.GetAllExternalLinks(l => l.EntityTypeGuid == Constants.Module.DocumentKindTypeGuid && Equals(l.EntityId, _obj.Id))
        .Select(s => s.ExternalEntityId)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Определить, относится ли вид документа к документам МКДО, созданным при инициализации.
    /// </summary>
    /// <returns>True, если вид документа создан при инициализации.</returns>
    public bool IsExchangeNativeDocumentKind()
    {
      return Domain.ModuleFunctions.GetAllExternalLinks(l => l.ExternalSystemId == Constants.Module.InitializeExternalLinkSystem && Equals(l.EntityId, _obj.Id) &&
                                                        (l.ExternalEntityId == Constants.DocumentKind.ContractStatementKind.ToString() ||
                                                         l.ExternalEntityId == Constants.DocumentKind.IncomingTaxInvoiceKind.ToString() ||
                                                         l.ExternalEntityId == Constants.DocumentKind.OutgoingTaxInvoiceKind.ToString() ||
                                                         l.ExternalEntityId == Constants.DocumentKind.UniversalBasicKind.ToString() ||
                                                         l.ExternalEntityId == Constants.DocumentKind.UniversalTaxInvoiceAndBasicKind.ToString() ||
                                                         l.ExternalEntityId == Constants.DocumentKind.WaybillDocumentKind.ToString() ||
                                                         l.ExternalEntityId == Docflow.Constants.Module.Initialize.ExchangeKind.ToString() ||
                                                         l.ExternalEntityId == Docflow.Constants.Module.Initialize.FormalizedPowerOfAttorneyKind.ToString()))
        .Any();
    }
    
    /// <summary>
    /// Получить виды документов.
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDocumentKind> GetDocumentKinds()
    {
      var approvalAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      return DocumentKinds.GetAll().Where(k => k.Status == Docflow.DocumentKind.Status.Active && k.AvailableActions.Any(a => a.Action == approvalAction));
    }
    
    /// <summary>
    /// Признак того, что есть виды документов с незаполненным кодом.
    /// </summary>
    /// <returns>True - если есть виды документов без кода, иначе - false.</returns>
    [Remote(IsPure = true)]
    public static bool HasDocumentKindWithNullCode()
    {
      return DocumentKinds.GetAll().Any(x => x.Status == CoreEntities.DatabookEntry.Status.Active && x.Code == null);
    }
    
    /// <summary>
    /// Получить виды документов.
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IDocumentKind> GetAllDocumentKinds()
    {
      return DocumentKinds.GetAll();
    }

    /// <summary>
    /// Выдать права по умолчанию на вид документа.
    /// </summary>
    public virtual void GrantDefaultAccessRightDocumentKind()
    {
      _obj.AccessRights.Grant(Roles.AllUsers, Constants.DocumentKind.DocumentKindChoiseAccessRightType);
      // Если выдали права в событии создания, то платформа не добавляет автора с полными правами.
      _obj.AccessRights.Grant(Users.Current, DefaultAccessRightsTypes.FullAccess);
    }
  }
}