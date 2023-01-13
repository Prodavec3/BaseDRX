using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Init = Sungero.FinancialArchive.Constants.Module.Initialize;

namespace Sungero.FinancialArchive.Server
{
  public class ModuleFunctions
  {
    #region МКДО
    
    /// <summary>
    /// Создать накладную.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static IWaybill CreateWaybillDocument(string comment, Sungero.ExchangeCore.IBoxBase box, Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var waybill = CreateDocument<IWaybill>(comment, box, counterparty, info);
      waybill.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.WaybillDocumentKind);
      if (kind != null)
        waybill.DocumentKind = kind;

      return waybill;
    }
    
    /// <summary>
    /// Создать счёт-фактуру полученный.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static FinancialArchive.IIncomingTaxInvoice CreateIncomingTaxInvoiceDocument(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                        Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                        Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var incomingTaxInvoice = CreateDocument<IIncomingTaxInvoice>(comment, box, counterparty, info);
      incomingTaxInvoice.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice;
      incomingTaxInvoice.IsAdjustment = isAdjustment;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.IncomingTaxInvoiceKind);
      if (kind != null)
        incomingTaxInvoice.DocumentKind = kind;

      if (corrected != null)
      {
        incomingTaxInvoice.Corrected = corrected;
        incomingTaxInvoice.LeadingDocument = corrected.LeadingDocument;
      }
      
      return incomingTaxInvoice;
    }
    
    /// <summary>
    /// Создать счёт-фактуру выставленный.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static FinancialArchive.IOutgoingTaxInvoice CreateOutgoingTaxInvoiceDocument(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                        Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                        Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var outgoingTaxInvoice = CreateDocument<IOutgoingTaxInvoice>(comment, box, counterparty, info);
      outgoingTaxInvoice.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice;
      outgoingTaxInvoice.IsAdjustment = isAdjustment;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.OutgoingTaxInvoiceKind);
      if (kind != null)
        outgoingTaxInvoice.DocumentKind = kind;

      if (corrected != null)
      {
        outgoingTaxInvoice.Corrected = corrected;
        outgoingTaxInvoice.LeadingDocument = corrected.LeadingDocument;
      }

      return outgoingTaxInvoice;
    }
    
    /// <summary>
    /// Создать акт.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static IContractStatement CreateContractStatementDocument(string comment, Sungero.ExchangeCore.IBoxBase box, Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var contractStatement = CreateDocument<IContractStatement>(comment, box, counterparty, info);
      contractStatement.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Act;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.ContractStatementKind);
      if (kind != null)
        contractStatement.DocumentKind = kind;
      
      return contractStatement;
    }
    
    /// <summary>
    /// Создать универсальный передаточный документ СЧФДОП.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static Docflow.IAccountingDocumentBase CreateUniversalTaxInvoiceAndBasic(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                    Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                    Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var universalDocument = CreateDocument<IUniversalTransferDocument>(comment, box, counterparty, info);
      universalDocument.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      universalDocument.IsAdjustment = isAdjustment;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.UniversalTaxInvoiceAndBasicKind);
      if (kind != null)
        universalDocument.DocumentKind = kind;
      
      if (corrected != null)
      {
        universalDocument.Corrected = corrected;
        universalDocument.LeadingDocument = corrected.LeadingDocument;
      }

      return universalDocument;
    }
    
    /// <summary>
    /// Создать универсальный передаточный документ ДОП.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static Docflow.IAccountingDocumentBase CreateUniversalBasic(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                       Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                       Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var universalDocument = CreateDocument<IUniversalTransferDocument>(comment, box, counterparty, info);
      universalDocument.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      universalDocument.IsAdjustment = isAdjustment;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.UniversalBasicKind);
      if (kind != null)
        universalDocument.DocumentKind = kind;
      
      if (corrected != null)
      {
        universalDocument.Corrected = corrected;
        universalDocument.LeadingDocument = corrected.LeadingDocument;
      }
      
      return universalDocument;
    }

    /// <summary>
    /// Создать документ из МКДО.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    private static T CreateDocument<T>(string comment, Sungero.ExchangeCore.IBoxBase box,
                                       Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info) where T : Docflow.IAccountingDocumentBase
    {
      var exchangeDoc = Sungero.Docflow.Shared.OfficialDocumentRepository<T>.Create();
      
      if (!string.IsNullOrEmpty(comment) && comment.Length > exchangeDoc.Info.Properties.Note.Length)
        comment = comment.Substring(0, exchangeDoc.Info.Properties.Note.Length);
      
      exchangeDoc.IsFormalized = true;
      exchangeDoc.Note = comment;
      exchangeDoc.BusinessUnit = ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(box);
      exchangeDoc.Counterparty = counterparty;

      return exchangeDoc;
    }
    
    #endregion
    
    #region Поиск по гиперссылкам
    
    /// <summary>
    /// Найти бухгалтерский документ.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="incomingTaxInvoice">True, если искать среди счетов-фактур полученных.</param>
    /// <param name="outgoingTaxInvoice">True, если искать среди счетов-фактур выставленных.</param>
    /// <param name="contractStatement">True, если искать среди актов.</param>
    /// <param name="waybill">True, если искать среди накладных.</param>
    /// <param name="universalTransferDocument">True, если искать среди УПД.</param>
    /// <returns>Список бухгалтерских документов.</returns>
    [Remote(IsPure = true)]
    public List<IAccountingDocumentBase> FindAccountingDocuments(string number, string date,
                                                                 string butin, string butrrc,
                                                                 string cuuid, string ctin, string ctrrc,
                                                                 bool corrective,
                                                                 bool incomingTaxInvoice,
                                                                 bool outgoingTaxInvoice,
                                                                 bool contractStatement,
                                                                 bool waybill,
                                                                 bool universalTransferDocument)
    {
      var result = AccountingDocumentBases.GetAll()
        .Where(a => incomingTaxInvoice && IncomingTaxInvoices.Is(a) ||
               outgoingTaxInvoice && OutgoingTaxInvoices.Is(a) ||
               contractStatement && ContractStatements.Is(a) ||
               waybill && Waybills.Is(a) ||
               universalTransferDocument && UniversalTransferDocuments.Is(a));
      
      // Фильтр по НОР.
      if (string.IsNullOrWhiteSpace(butin) || string.IsNullOrWhiteSpace(butrrc))
        return new List<IAccountingDocumentBase>();
      
      var businessUnit = Sungero.Company.BusinessUnits.GetAll().FirstOrDefault(x => x.TIN == butin && x.TRRC == butrrc);
      if (businessUnit == null)
        return new List<IAccountingDocumentBase>();
      else
        result = result.Where(x => Equals(x.BusinessUnit, businessUnit));
      
      // Фильтр по номеру.
      var relevantNumbers = this.GetRelevantNumbers(number);
      result = result.Where(x => relevantNumbers.Contains(x.RegistrationNumber));
      
      // Фильтр по дате.
      DateTime parsedDate;
      if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date,
                                                                     "dd'.'MM'.'yyyy",
                                                                     System.Globalization.CultureInfo.InvariantCulture,
                                                                     System.Globalization.DateTimeStyles.None,
                                                                     out parsedDate))
        result = result.Where(x => x.RegistrationDate == parsedDate);
      
      // Фильтр по контрагенту.
      var counterparties = Sungero.Parties.PublicFunctions.Module.Remote.FindCounterparty(cuuid, ctin, ctrrc, string.Empty);
      if (counterparties.Any())
        result = result.Where(x => counterparties.Contains(x.Counterparty));
      
      // Фильтр корректировочный или нет.
      result = result.Where(x => x.IsAdjustment == corrective);
      
      return result.ToList();
    }
    
    /// <summary>
    /// Получить список номеров, соответствующих заданному рег. номеру документа.
    /// </summary>
    /// <param name="number">Рег. номер.</param>
    /// <returns>Список номеров, соответствующих заданному.</returns>
    private List<string> GetRelevantNumbers(string number)
    {
      var relevantNumbers = new List<string>();
      relevantNumbers.Add(number);
      
      // Регулярное выражение соответствует первому вхождению нуля или группы нулей.
      var leadZerosRegex = new System.Text.RegularExpressions.Regex("0+");
      
      // Поиск в конце номера подстроки, состоящей только из цифр.
      var pattern = @"\d+$";
      System.Text.RegularExpressions.Match isMatch = System.Text.RegularExpressions.Regex.Match(number, pattern);
      if (isMatch.Success)
      {
        // Если подстрока найдена, то удаляем ведущие нули в ней и добавляем в список номеров.
        relevantNumbers.Add(leadZerosRegex.Replace(isMatch.Value, string.Empty, 1));
      }
      
      // Получаем номер с префиксом, но без ведущих нулей.
      pattern = @"^\D*0+\d+$";
      isMatch = System.Text.RegularExpressions.Regex.Match(number, pattern);
      if (isMatch.Success)
      {
        relevantNumbers.Add(leadZerosRegex.Replace(number, string.Empty, 1));
      }
      return relevantNumbers;
    }
    
    #endregion
    
    #region Импорт формализованных документов
    
    /// <summary>
    /// Загрузить формализованный документ из XML.
    /// </summary>
    /// <param name="file">XML.</param>
    /// <param name="requireFtsId">Соотносить НОР и Контрагента только по ФНС-ИД.</param>
    /// <returns>Структура с созданным документом и его телами.</returns>
    [Remote, Public]
    public virtual Structures.Module.IImportResult ImportFormalizedDocument(Docflow.Structures.Module.IByteArray file, bool requireFtsId)
    {
      var result = Structures.Module.ImportResult.Create();
      var sellerTitle = FormalizeDocumentsParser.Extension.GetDocument<FormalizeDocumentsParser.ISellerTitle>(file.Bytes);
      if (sellerTitle != null)
      {
        var isGoodsTransfer = sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.GoodsTransferDocument;
        var isWorksTransfer = sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.WorksTransferDocument;
        var isUniversalTransfer = sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.UniversalTransferDocument;
        var isUniversalCorrection = sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.UniversalCorrectionTransferDocument;
        result.IsSuccess = true;
        
        Docflow.IAccountingDocumentBase document;
        if (isGoodsTransfer)
        {
          if (!FinancialArchive.Waybills.AccessRights.CanCreate())
          {
            result.Error = Resources.NoRightsToCreateDocumentFormat(FinancialArchive.Waybills.Info.LocalizedName);
            result.IsSuccess = false;
            return result;
          }
          document = FinancialArchive.Waybills.Create();
        }
        else if (isWorksTransfer)
        {
          if (!FinancialArchive.ContractStatements.AccessRights.CanCreate())
          {
            result.Error = Resources.NoRightsToCreateDocumentFormat(FinancialArchive.ContractStatements.Info.LocalizedName);
            result.IsSuccess = false;
            return result;
          }
          document = FinancialArchive.ContractStatements.Create();
        }
        else if (sellerTitle.Function == FormalizeDocumentsParser.UniversalDocumentFunction.Schf)
        {
          if (!FinancialArchive.OutgoingTaxInvoices.AccessRights.CanCreate())
          {
            result.Error = Resources.NoRightsToCreateDocumentFormat(FinancialArchive.OutgoingTaxInvoices.Info.LocalizedName);
            result.IsSuccess = false;
            return result;
          }
          document = FinancialArchive.OutgoingTaxInvoices.Create();
        }
        else
        {
          if (!FinancialArchive.UniversalTransferDocuments.AccessRights.CanCreate())
          {
            result.Error = Resources.NoRightsToCreateDocumentFormat(FinancialArchive.UniversalTransferDocuments.Info.LocalizedName);
            result.IsSuccess = false;
            return result;
          }
          document = FinancialArchive.UniversalTransferDocuments.Create();
        }
        
        // НОР документа должна подбираться по параметрам, а не от сотрудника.
        document.BusinessUnit = null;
        
        result.Document = document;
        
        result.BusinessUnitName = sellerTitle.Seller.GetName();
        result.BusinessUnitTin = sellerTitle.Seller.Tin;
        result.BusinessUnitTrrc = sellerTitle.Seller.Trrc;
        result.BusinessUnitType = sellerTitle.Seller.Type.ToString();
        result.CounterpartyName = sellerTitle.Buyer.GetName();
        result.CounterpartyTin = sellerTitle.Buyer.Tin;
        result.CounterpartyTrrc = sellerTitle.Buyer.Trrc;
        result.CounterpartyType = sellerTitle.Buyer.Type.ToString();
        result.SenderFtsId = sellerTitle.SenderId;
        result.ReceiverFtsId = sellerTitle.ReceiverId;
        
        document.IsAdjustment = sellerTitle.IsAdjustment;
        document.IsRevision = sellerTitle.IsRevision;
        document.TotalAmount = (double?)sellerTitle.TotalAmount;
        
        if (sellerTitle.Function.HasValue)
        {
          switch (sellerTitle.Function.Value)
          {
            case FormalizeDocumentsParser.UniversalDocumentFunction.Schf:
              document.FormalizedFunction = Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
              document.DocumentKind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.OutgoingTaxInvoiceKind);
              break;
            case FormalizeDocumentsParser.UniversalDocumentFunction.SchfDop:
              document.FormalizedFunction = Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
              document.DocumentKind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.UniversalTaxInvoiceAndBasicKind);
              break;
            case FormalizeDocumentsParser.UniversalDocumentFunction.Dop:
              document.FormalizedFunction = Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
              document.DocumentKind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.UniversalBasicKind);
              break;
            default:
              throw new Exception("Invalid value for UniversalDocumentFunction");
          }
        }

        if (document.TotalAmount.HasValue)
          document.Currency = Commons.Currencies.GetAll().SingleOrDefault(c => c.NumericCode == sellerTitle.CurrencyCode);
        
        FillBusinessUnitAndCounterparty(document, sellerTitle, requireFtsId);
        result.IsBusinessUnitFound = document.BusinessUnit != null;
        result.IsCounterpartyFound = document.Counterparty != null;
        result.IsCounterpartyCanExchange = document.Counterparty != null && document.Counterparty.CanExchange == true;
        if (document.BusinessUnit == null || document.Counterparty == null)
        {
          if (document.BusinessUnit == null)
          {
            result.Error = requireFtsId ?
              Resources.ImportDialog_NoBusinessUnitWithFTSIdFormat(sellerTitle.Seller.GetName(), sellerTitle.Seller.Tin, sellerTitle.Seller.Trrc, sellerTitle.SenderId) :
              Resources.ImportDialog_NoBusinessUnitWithTINFormat(sellerTitle.Seller.GetName(), sellerTitle.Seller.Tin, sellerTitle.Seller.Trrc);
            result.IsSuccess = false;
            return result;
          }
          if (document.Counterparty == null)
          {
            result.Error = requireFtsId ?
              Resources.ImportDialog_NoCounterpartyWithFTSIdFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc, sellerTitle.ReceiverId) :
              Resources.ImportDialog_NoCounterpartyWithTINFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc);
            result.IsSuccess = false;
            return result;
          }
        }
        else if (document.Counterparty.CanExchange != true)
        {
          result.Error = Resources.ImportDialog_NoExchangeWithCounterpartyFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc);
          result.IsSuccess = false;
          return result;
        }
        
        if (isGoodsTransfer)
          document.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer;
        else if (isWorksTransfer)
          document.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer;
        else
          document.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
        
        result.Body = sellerTitle.Body;
        
        var version = document.Versions.AddNew();
        version.AssociatedApplication = Content.AssociatedApplications.GetByExtension("pdf");
        version.BodyAssociatedApplication = Content.AssociatedApplications.GetByExtension("xml");
        document.SellerTitleId = version.Id;
        
        if (FinancialArchive.Waybills.Is(document) ||
            FinancialArchive.ContractStatements.Is(document) ||
            FinancialArchive.UniversalTransferDocuments.Is(document))
          version.Note = FinancialArchive.Resources.SellerTitleVersionNote;
        
        document.IsFormalized = true;
        
        var number = sellerTitle.IsAdjustment ? sellerTitle.CorrectionNumber : sellerTitle.Number;
        var date = sellerTitle.IsAdjustment ? sellerTitle.CorrectionDate : sellerTitle.Date;
        var isRegistered = Docflow.PublicFunctions.OfficialDocument.TryExternalRegister(document, number, date);
        
        document.IsFormalizedSignatoryEmpty = document.IsFormalized == true && !FormalizeDocumentsParser.SellerSignatoryInfo.HasSellerSignatoryInfo(sellerTitle.Body);
        document.Subject = string.Empty;
        if (document.IsRevision == true)
          document.Note = string.Format(Exchange.Resources.TaxInvoiceRevision, sellerTitle.RevisionNumber, sellerTitle.RevisionDate.Value.Date.ToString("d"));
        else if (isUniversalCorrection)
          document.Note = string.Format(Exchange.Resources.TaxInvoiceTo, sellerTitle.Number, sellerTitle.Date.Value.Date.ToString("d"));
        
        if (!isRegistered || document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
        {
          if (isUniversalTransfer)
            document.Note = string.Format(Exchange.Resources.TaxInvoice, number, date.Value.Date.ToString("d")) +
              Environment.NewLine + document.Note;
          else if (isUniversalCorrection)
            document.Note = string.Format(Exchange.Resources.TaxInvoiceCorrection, number, date.Value.Date.ToString("d")) +
              Environment.NewLine + document.Note;
          else
            document.Note = string.Format(Exchange.Resources.IncomingNotNumeratedDocumentNote, date.Value.Date.ToString("d"), number) +
              Environment.NewLine + document.Note;
        }
        if (isUniversalCorrection && !string.IsNullOrEmpty(sellerTitle.Number) && sellerTitle.Date != null)
        {
          if (sellerTitle.Function == FormalizeDocumentsParser.UniversalDocumentFunction.Schf)
          {
            document.Corrected = Sungero.FinancialArchive.OutgoingTaxInvoices.GetAll()
              .Where(x => x.RegistrationNumber == sellerTitle.Number &&
                     x.RegistrationDate == sellerTitle.Date.Value && Equals(x.Counterparty, document.Counterparty))
              .FirstOrDefault();
          }
          else
          {
            document.Corrected = Sungero.FinancialArchive.UniversalTransferDocuments.GetAll()
              .Where(x => x.RegistrationNumber == sellerTitle.Number &&
                     x.RegistrationDate == sellerTitle.Date.Value && Equals(x.Counterparty, document.Counterparty))
              .FirstOrDefault();
          }
        }
        
        result.PublicBody = Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedXml(Docflow.Structures.Module.ByteArray.Create(result.Body)).Bytes;
        return result;
      }
      result.Error = Resources.ImportDialog_CannotRecognizeDocument;
      result.IsSuccess = false;
      return result;
    }
    
    /// <summary>
    /// Проверить, заполнена ли в титуле продавца информация о подписывающем.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если уже есть информация о подписывающем.</returns>
    [Remote, Public]
    public virtual bool HasSellerSignatoryInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document.IsFormalized != true)
        return false;
      
      using (var body = document.Versions.Single(v => v.Id == document.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          memory.Position = 0;
          return FormalizeDocumentsParser.SellerSignatoryInfo.HasSellerSignatoryInfo(memory);
        }
      }
    }
    
    /// <summary>
    /// Проверить, заполнены ли в титуле продавца ФНС Ид отправителя и получателя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если уже есть информация о ФНС.</returns>
    [Remote, Public]
    public virtual bool HasSellerTitleInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document.IsFormalized != true)
        return false;
      
      using (var body = document.Versions.Single(v => v.Id == document.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          return FormalizeDocumentsParser.SellerTitleInfo.HasRequiredProperties(memory.ToArray());
        }
      }
    }
    
    /// <summary>
    /// Получить сервис обмена.
    /// </summary>
    /// <param name="title">Титул продавца.</param>
    /// <returns>Сервис обмена.</returns>
    private static Enumeration? GetExchangeService(FormalizeDocumentsParser.IFormalizedDocument title)
    {
      switch (title.FromService)
      {
        case FormalizeDocumentsParser.SupportedService.Diadoc:
          return ExchangeCore.ExchangeService.ExchangeProvider.Diadoc;
        case FormalizeDocumentsParser.SupportedService.Sbis:
          return ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
        default:
          Logger.DebugFormat("GetExchangeService. Unsupportable exchange service {0}", title.FromService.ToString());
          return null;
      }
    }
    
    /// <summary>
    /// Заполнить в документе НОР и контрагента.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="requireFtsId">Соотносить НОР и контрагента только по ФНС ИД.</param>
    private static void FillBusinessUnitAndCounterparty(Docflow.IAccountingDocumentBase document,
                                                        FormalizeDocumentsParser.ISellerTitle sellerTitle,
                                                        bool requireFtsId)
    {
      ExchangeCore.IBusinessUnitBox box = null;
      Parties.ICounterparty counterparty = null;
      
      // Поиск по ФНС ИД.
      if (!string.IsNullOrWhiteSpace(sellerTitle.SenderId) || !string.IsNullOrWhiteSpace(sellerTitle.ReceiverId))
      {
        var exchangeProvider = GetExchangeService(sellerTitle);
        var boxes = ExchangeCore.BusinessUnitBoxes.GetAll()
          .Where(b => b.ExchangeService.ExchangeProvider == exchangeProvider)
          .ToList();

        box = boxes.Where(b => b.FtsId == sellerTitle.SenderId).SingleOrDefault();
        if (box != null)
          counterparty = GetCounterparty(sellerTitle.ReceiverId, box);
      }
      
      // Поиск по ИНН \ КПП.
      if (!requireFtsId && (box == null || counterparty == null))
      {
        var boxOnTin = GetBox(sellerTitle.Seller);
        if (boxOnTin != null)
        {
          if (counterparty == null)
            counterparty = GetCounterparty(sellerTitle.Buyer, boxOnTin, true);
          if (box == null)
            box = boxOnTin;
        }
      }
      
      // Если нашелся ящик - заполняем.
      if (box != null)
      {
        document.BusinessUnitBox = box;
        document.BusinessUnit = box.BusinessUnit;
        document.Counterparty = counterparty;
      }
      
      if (!requireFtsId && (box == null || counterparty == null))
      {
        // Ищем по ИНН \ КПП, не связываясь с МКДО.
        var unit = GetBusinessUnit(sellerTitle.Seller);
        if (unit != null)
        {
          if (document.Counterparty == null)
            document.Counterparty = GetCounterparty(sellerTitle.Buyer, null, false);
          if (document.BusinessUnit == null)
            document.BusinessUnit = unit;
        }
      }
    }
    
    /// <summary>
    /// Получить абонентский ящик участника ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <returns>Абонентский ящик.</returns>
    private static ExchangeCore.IBusinessUnitBox GetBox(FormalizeDocumentsParser.IExchangeParticipant participant)
    {
      var boxes = ExchangeCore.BusinessUnitBoxes.GetAll()
        .Where(b => b.BusinessUnit.TIN == participant.Tin)
        .ToList();
      if (boxes.Count > 1)
        boxes = boxes.Where(b => b.BusinessUnit.TRRC == participant.Trrc).ToList();
      if (boxes.Count > 1)
        boxes = boxes.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      if (boxes.Any())
        return boxes.Single();
      return null;
    }
    
    /// <summary>
    /// Определить НОР по информации об участнике ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <returns>Наша организация.</returns>
    private static Company.IBusinessUnit GetBusinessUnit(FormalizeDocumentsParser.IExchangeParticipant participant)
    {
      var units = Company.BusinessUnits.GetAll()
        .Where(b => b.TIN == participant.Tin)
        .ToList();
      if (units.Count > 1)
        units = units.Where(b => b.TRRC == participant.Trrc).ToList();
      if (units.Count > 1)
        units = units.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      if (units.Any())
        return units.Single();
      return null;
    }
    
    /// <summary>
    /// Определить контрагента по информации об участнике ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="canExchange">Участвует ли в электронном обмене.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterparty(FormalizeDocumentsParser.IExchangeParticipant participant,
                                                         ExchangeCore.IBusinessUnitBox box, bool canExchange)
    {
      var counterparties = Parties.Counterparties.GetAll()
        .Where(b => b.TIN == participant.Tin)
        .ToList();
      if (canExchange)
        counterparties = counterparties.Where(c => c.ExchangeBoxes.Any(b => b.Box == box)).ToList();
      if (counterparties.Count > 1)
        counterparties = counterparties.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      if (counterparties.Count > 1)
      {
        if (!string.IsNullOrWhiteSpace(participant.Trrc))
          counterparties = counterparties.OfType<Parties.ICompanyBase>().Where(b => b.TRRC == participant.Trrc).ToList<Parties.ICounterparty>();
        else
          counterparties = counterparties.Where(b => !Parties.CompanyBases.Is(b)).ToList();
      }
      if (counterparties.Count > 1 && counterparties.Any(b => b.CanExchange == true))
      {
        counterparties = counterparties.Where(b => b.CanExchange == true).ToList();
      }
      if (counterparties.Any())
        return counterparties.Single();
      return null;
    }
    
    /// <summary>
    /// Определить контрагента по ФНС ИД и абонентскому ящику.
    /// </summary>
    /// <param name="ftsId">ФНС ИД.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterparty(string ftsId, ExchangeCore.IBusinessUnitBox box)
    {
      return Parties.Counterparties.GetAll()
        .Where(c => c.ExchangeBoxes.Any(b => b.FtsId == ftsId && b.Box == box))
        .SingleOrDefault();
    }
    
    /// <summary>
    /// Сгенерировать ФНС-ид (и связанные свойства) для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <remarks>Документу будет перезаписано тело версии.</remarks>
    [Public, Remote]
    public virtual void AddOrReplaceSellerTitleInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document == null || !document.HasVersions)
        return;
      
      var version = document.Versions.SingleOrDefault(v => v.Id == document.SellerTitleId);
      if (version == null)
        return;
      
      using (var memory = new System.IO.MemoryStream())
      {
        version.Body.Read().CopyTo(memory);
        var newBody = AddOrReplaceSellerTitleInfo(memory, document.BusinessUnitBox, document.Counterparty,
                                                  document.FormalizedServiceType.Value, document.IsAdjustment == true);
        version.Body.Write(newBody);
        document.Save();
      }
    }
    
    /// <summary>
    /// Сгенерировать ФНС-ид (и связанные свойства) для документа.
    /// </summary>
    /// <param name="stream">Поток с XML в исходном виде.</param>
    /// <param name="rootBox">Ящик, через который будет отправлен документ.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="formalizedServiceType">Тип документа (УПД, ДПТ, ДПРР).</param>
    /// <param name="isAdjustment">Корректировочный (важно только для УКД).</param>
    /// <returns>Сгенерированный XML новым потоком.</returns>
    private static System.IO.MemoryStream AddOrReplaceSellerTitleInfo(System.IO.MemoryStream stream, ExchangeCore.IBusinessUnitBox rootBox,
                                                                      Parties.ICounterparty counterparty, Sungero.Core.Enumeration formalizedServiceType,
                                                                      bool isAdjustment)
    {
      var counterpartyExchange = counterparty.ExchangeBoxes.SingleOrDefault(c => Equals(c.Box, rootBox));
      if (counterpartyExchange == null)
        throw AppliedCodeException.Create(string.Format("Counterparty {0} must have exchange from box {1}", counterparty.Id, rootBox.Id));
      
      var sellerTitleInfo = new FormalizeDocumentsParser.SellerTitleInfo();
      if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer)
        sellerTitleInfo.DocumentType = FormalizeDocumentsParser.DocumentType.GoodsTransferDocument;
      else if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
        sellerTitleInfo.DocumentType = FormalizeDocumentsParser.DocumentType.WorksTransferDocument;
      else if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
        sellerTitleInfo.DocumentType = isAdjustment ? FormalizeDocumentsParser.DocumentType.UniversalCorrectionTransferDocument : FormalizeDocumentsParser.DocumentType.UniversalTransferDocument;

      if (rootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        sellerTitleInfo.Operator = FormalizeDocumentsParser.SupportedEdoOperators.Diadoc;
      else if (rootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        sellerTitleInfo.Operator = FormalizeDocumentsParser.SupportedEdoOperators.Sbis;
      else
        Logger.DebugFormat("AddOrReplaceSellerTitleInfo. Unsupportable exchange service {0}", rootBox.ExchangeService.ExchangeProvider);
      
      sellerTitleInfo.Receiver = counterpartyExchange.FtsId;
      sellerTitleInfo.Sender = rootBox.FtsId;
      return sellerTitleInfo.AddOrReplaceToXml(stream);
    }
    
    /// <summary>
    /// Сгенерировать титул продавца.
    /// </summary>
    /// <param name="statement">Документ, для которого генерируется титул.</param>
    /// <param name="sellerTitle">Информация о титуле продавца.</param>
    [Public, Remote]
    public static void GenerateSellerTitle(Docflow.IAccountingDocumentBase statement, Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      if (statement.IsFormalized != true)
        return;
      
      var sellerSignatoryInfo = new FormalizeDocumentsParser.SellerSignatoryInfo();
      sellerSignatoryInfo.CompanyName = statement.BusinessUnit.LegalName;
      sellerSignatoryInfo.FirstName = sellerTitle.Signatory.Person.FirstName;
      sellerSignatoryInfo.LastName = sellerTitle.Signatory.Person.LastName;
      sellerSignatoryInfo.MiddleName = sellerTitle.Signatory.Person.MiddleName;
      sellerSignatoryInfo.JobTitle = Functions.Module.GetSellerJobTitle(sellerTitle);
      
      // Лицо, совершившее сделку и ответственное за ее оформление.
      if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister)
        sellerSignatoryInfo.Powers = Constants.Module.SellerTitlePowers.MadeAndSignOperation;
      // Лицо, ответственное за оформление свершившегося события.
      else if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register)
        sellerSignatoryInfo.Powers = Constants.Module.SellerTitlePowers.PersonDocumentedOperation;
      // Лицо, совершившее сделку и операцию, ответственное за ее оформление и за подписание счетов-фактур.
      else if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegisterAndInvoiceSignatory)
        sellerSignatoryInfo.Powers = Constants.Module.SellerTitlePowers.MadeAndResponsibleForOperationAndSignedInvoice;
      // Лицо, ответственное за оформление свершившегося события и за подписание счетов-фактур.
      else if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory)
        sellerSignatoryInfo.Powers = Constants.Module.SellerTitlePowers.ResponsibleForOperationAndSignatoryForInvoice;
      
      sellerSignatoryInfo.PowersBase = sellerTitle.SignatureSetting != null ? Docflow.PublicFunctions.Module.GetSigningReason(sellerTitle.SignatureSetting) :
        Docflow.SignatureSettings.Info.Properties.Reason.GetLocalizedValue(Docflow.SignatureSetting.Reason.Duties);

      sellerSignatoryInfo.TIN = statement.BusinessUnit.TIN;
      
      using (var body = statement.Versions.Single(v => v.Id == statement.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          memory.Position = 0;
          using (var patchedXml = sellerSignatoryInfo.AddOrReplaceToXml(memory))
          {
            if (!HasUnsignedSellerTitle(statement))
            {
              // При создании версии чистится статус эл. обмена, восстанавливаем его.
              var exchangeState = statement.ExchangeState;
              statement.CreateVersion();
              statement.ExchangeState = exchangeState;
            }
            
            var version = statement.LastVersion;
            statement.SellerTitleId = version.Id;
            statement.OurSignatory = sellerTitle.Signatory;
            statement.OurSigningReason = sellerTitle.SignatureSetting;
            version.Body.Write(patchedXml);
            statement.IsFormalizedSignatoryEmpty = false;
            statement.Save();
          }
        }
      }
    }
    
    /// <summary>
    /// Получить наименование должности для титула продавца.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Наименование должности.</returns>
    public virtual string GetSellerJobTitle(Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      if (sellerTitle == null)
        return null;
      
      var settingJobTitle = sellerTitle.SignatureSetting != null && sellerTitle.SignatureSetting.JobTitle != null ? sellerTitle.SignatureSetting.JobTitle.Name : null;
      var signatoryJobTitle = sellerTitle.Signatory.JobTitle != null ? sellerTitle.Signatory.JobTitle.Name : null;
      return Docflow.PublicFunctions.Module.CutText(settingJobTitle != null ? settingJobTitle : signatoryJobTitle,
                                                    Docflow.PublicConstants.AccountingDocumentBase.JobTitleMaxLength);
    }
    
    /// <summary>
    /// Определить, есть ли у документа неподписанный титул продавца.
    /// </summary>
    /// <param name="statement">Документ.</param>
    /// <returns>True, если есть неподписанный титул продавца, иначе - false.</returns>
    [Public, Remote]
    public static bool HasUnsignedSellerTitle(Docflow.IAccountingDocumentBase statement)
    {
      if (statement.SellerTitleId != null)
      {
        var existingSellerTitle = statement.Versions.Where(x => x.Id == statement.SellerTitleId).FirstOrDefault();
        if (existingSellerTitle != null && !Signatures.Get(existingSellerTitle).Any())
          return true;
      }
      
      return false;
    }
    
    #endregion
    
    #region Выгрузка документа из финархива

    /// <summary>
    /// Получить ИД подписи отправителя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>ИД подписи отправителя.</returns>
    [Public]
    public virtual int? GetSenderSignatureId(IOfficialDocument document, Sungero.Content.IElectronicDocumentVersions version)
    {
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, version.Id);
      var senderSignature = Signatures.Get(version).Where(x => x.Id == info.SenderSignId).SingleOrDefault();
      if (senderSignature != null)
        return senderSignature.Id;
      else
        return null;
    }

    /// <summary>
    /// Получить ИД подписи получателя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>ИД подписи получателя.</returns>
    [Public]
    public virtual int? GetReceiverSignatureId(IOfficialDocument document, Sungero.Content.IElectronicDocumentVersions version)
    {
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, version.Id);
      var senderSignature = Signatures.Get(version).Where(x => x.Id == info.ReceiverSignId).SingleOrDefault();
      if (senderSignature != null)
        return senderSignature.Id;
      else
        return null;
    }
    
    #endregion
  }
}