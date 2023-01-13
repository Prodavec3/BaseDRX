using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentInfo;

namespace Sungero.Exchange.Server
{
  partial class ExchangeDocumentInfoFunctions
  {
    /// <summary>
    /// Получить все записи информации о документе обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Записи информации.</returns>
    public static IQueryable<IExchangeDocumentInfo> GetAllExDocumentInfos(Docflow.IOfficialDocument document)
    {
      return ExchangeDocumentInfos.GetAll(x => Equals(x.Document, document));
    }
    
    /// <summary>
    /// Получить запись информации о документе обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Запись информации.</returns>
    [Public, Remote(IsPure = true)]
    public static IExchangeDocumentInfo GetExDocumentInfoFromVersion(Docflow.IOfficialDocument document, int versionId)
    {
      return GetAllExDocumentInfos(document).Where(e => e.VersionId == versionId).OrderByDescending(e => e.MessageDate).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверить, отправляли ли уже последнюю версию документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если уже была отправка.</returns>
    /// <remarks>Работает по истории документа.</remarks>
    [Public, Remote(IsPure = true)]
    public static bool LastVersionSended(Docflow.IOfficialDocument document)
    {
      if (document == null || !document.HasVersions)
        return false;
      
      var documentSent = new Enumeration(Constants.Module.Exchange.SendDocument);
      var answerSent = new Enumeration(Constants.Module.Exchange.SendAnswer);
      var versionNumber = document.LastVersion.Number;
      if (Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true)
        return document.History.GetAll().Any(h => (h.Operation == documentSent || h.Operation == answerSent));
      return document.History.GetAll().Any(h => (h.Operation == documentSent || h.Operation == answerSent) && h.VersionNumber == versionNumber);
    }
    
    /// <summary>
    /// Проверить, отправляли ли уже последнюю версию документа определённому контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>True, если уже была отправка.</returns>
    [Public, Remote(IsPure = true)]
    public static bool LastVersionSended(Docflow.IOfficialDocument document, ExchangeCore.IBusinessUnitBox box, Parties.ICounterparty counterparty)
    {
      if (document == null || counterparty == null || box == null || !document.HasVersions)
        return false;
      
      return GetAllExDocumentInfos(document)
        .Any(x =>
             x.VersionId == document.LastVersion.Id &&
             x.MessageType == MessageType.Outgoing &&
             Equals(x.Counterparty, counterparty) &&
             Equals(x.Box, box));
    }
    
    /// <summary>
    /// Получить информацию о документе, который пришел от контрагента.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о документе обмена.</returns>
    [Public, Remote(IsPure = true)]
    public static IExchangeDocumentInfo GetIncomingExDocumentInfo(Docflow.IOfficialDocument document)
    {
      if (document.Versions.Any())
      {
        var incomingVersion = document.Versions.OrderBy(v => v.Number).First();
        return GetAllExDocumentInfos(document).FirstOrDefault(x => x.VersionId == incomingVersion.Id &&
                                                              x.MessageType == MessageType.Incoming);
      }
      return ExchangeDocumentInfos.Null;
    }
    
    /// <summary>
    /// Получить информацию о документе обмена ИД сервиса обмена.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="serviceId">ИД документа в сервисе обмена.</param>
    /// <returns>Информация о документе обмена.</returns>
    [Public]
    public static IExchangeDocumentInfo GetExDocumentInfoByExternalId(ExchangeCore.IBoxBase box, string serviceId)
    {
      var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      return ExchangeDocumentInfos.GetAll().OrderByDescending(x => x.Id).FirstOrDefault(x => Equals(x.RootBox, rootBox) && x.ServiceDocumentId == serviceId);
    }
    
    /// <summary>
    /// Получить последнюю информацию по документу.
    /// По последней отправленной\приятной версии для неформализованных и по единственной для формализованных.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о документе обмена.</returns>
    [Public, Remote(IsPure = true)]
    public static IExchangeDocumentInfo GetLastDocumentInfo(Docflow.IOfficialDocument document)
    {
      if (document.Versions.Any())
      {
        var infos = GetAllExDocumentInfos(document).ToList();
        if (!infos.Any())
          return null;
        
        // Сортируем версии по номеру.
        var versions = document.Versions.OrderByDescending(s => s.Number).ToList();
        foreach (var version in versions)
        {
          // Возвращаем версию, по которой последней создана информация.
          var versionInfo = infos.OrderByDescending(i => i.MessageDate).FirstOrDefault(i => i.VersionId == version.Id);
          if (versionInfo != null)
            return versionInfo;
        }
      }
      return null;
    }
    
    /// <summary>
    /// Получить контрагентов для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрагент, от которого пришел документ, либо контрагенты из карточки документа.</returns>
    [Public]
    public static List<Parties.ICounterparty> GetDocumentCounterparties(Docflow.IOfficialDocument document)
    {
      if (Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document))
      {
        var info = GetIncomingExDocumentInfo(document);
        if (info != null)
          return new List<Parties.ICounterparty>() { info.Counterparty };
      }
      
      return Docflow.PublicFunctions.OfficialDocument.GetCounterparties(document);
    }
    
    /// <summary>
    /// Получить контрагента по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>Контрагент, от которого пришел документ.</returns>
    [Public]
    public static Parties.ICounterparty GetDocumentCounterparty(Content.IElectronicDocument document, Content.IElectronicDocumentVersions version)
    {     
      var info = GetEchangeDocumentInfo(document, version);
      return info != null ? info.Counterparty : null;
    }
    
    /// <summary>
    /// Получить НОР для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>Наша организация, на которую пришел документ.</returns>
    [Public]
    public static Company.IBusinessUnit GetDocumentBusinessUnit(Content.IElectronicDocument document, Content.IElectronicDocumentVersions version)
    {
      var info = GetEchangeDocumentInfo(document, version);
      return info != null ? ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(info.Box) : null;
    }
    
    /// <summary>
    /// Получить сведения о документе обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>Сведения о документе обмена.</returns>
    public static IExchangeDocumentInfo GetEchangeDocumentInfo(Content.IElectronicDocument document, Content.IElectronicDocumentVersions version)
    {
      if (document == null)
        return null;
      if (version == null)
        return null;
      
      return ExchangeDocumentInfos.GetAll(x => Equals(x.Document, document) && Equals(x.VersionId, version.Id)).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сведения об организации, подписавшей документ, из сведений о документе обмена и подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Наименование и ИНН организации.</returns>
    [Public]
    public virtual Structures.Module.IOrganizationInfo GetSigningOrganizationInfo(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return Structures.Module.OrganizationInfo.Create();
      
      var isOurSignature = false;
      if (_obj.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Incoming)
        isOurSignature = _obj.ReceiverSignId == signature.Id;
      else
        isOurSignature = _obj.SenderSignId == signature.Id;
      
      var companyName = isOurSignature ? _obj.RootBox.BusinessUnit.Name : _obj.Counterparty.Name;
      var companyTin = isOurSignature ? _obj.RootBox.BusinessUnit.TIN : _obj.Counterparty.TIN;
      return Structures.Module.OrganizationInfo.Create(companyName, companyTin, isOurSignature);
    }
    
    /// <summary>
    /// Отпралять задания/уведомления ответственному.
    /// </summary>
    /// <returns>Признак отправки задания ответственному за ящик.</returns>
    [Public]
    public virtual bool NeedReceiveTask()
    {
      return true;
    }
  }
}