using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Exchange.Structures.Module
{
  /// <summary>
  /// Сервисный документ, сертификат, которым он должен быть подписан и подпись.
  /// </summary>
  partial class ReglamentDocumentWithCertificate
  {
    public string Name { get; set; }
    
    public byte[] Content { get; set; }
    
    public ICertificate Certificate { get; set; }
    
    public byte[] Signature { get; set; }
    
    public string ParentDocumentId { get; set; }
    
    public ExchangeCore.IBusinessUnitBox Box { get; set; }
    
    public Docflow.IOfficialDocument LinkedDocument { get; set; }
    
    public string ServiceMessageId { get; set; }
    
    public string ServiceDocumentId { get; set; }
    
    public string ServiceDocumentStageId { get; set; }
    
    public string ServiceCounterpartyId { get; set; }
    
    public bool IsRootDocumentReceipt { get; set; }
    
    public IExchangeDocumentInfo Info { get; set; }
    
    public bool IsInvoiceFlow { get; set; }
    
    public Sungero.Core.Enumeration? ReglamentDocumentType { get; set; }
    
    public string FormalizedPoAUnifiedRegNumber { get; set; }
  }
  
  partial class AddendumInfo
  {
    public Docflow.IOfficialDocument Addendum { get; set; }
    
    public bool NeedRejectFirstVersion { get; set; }
    
    public Sungero.Core.Enumeration? BuyerAcceptanceStatus { get; set; }
  }
  
  partial class SendToCounterpartyInfo
  {
    public List<ExchangeCore.IBusinessUnitBox> Boxes { get; set; }
    
    public List<Parties.ICounterparty> Counterparties { get; set; }
    
    public ExchangeCore.IBusinessUnitBox DefaultBox { get; set; }
    
    public string ParentDocumentId { get; set; }
    
    public Parties.ICounterparty DefaultCounterparty { get; set; }
    
    public Sungero.Exchange.Structures.Module.DocumentCertificatesInfo Certificates { get; set; }
    
    public List<Sungero.Exchange.Structures.Module.AddendumInfo> Addenda { get; set; }
    
    public bool HasAddendaToSend { get; set; }
    
    public string Error { get; set; }
    
    public bool HasError { get; set; }
    
    public bool IsSignedByUs { get; set; }
    
    public bool IsSignedByCounterparty { get; set; }
    
    public bool AnswerIsSent { get; set; }
    
    public bool NeedRejectFirstVersion { get; set; }
    
    public bool HasApprovalSignature { get; set; }
    
    public bool CanApprove { get; set; }
    
    public bool CanSendSignAsAnswer { get; set; }
    
    public bool CanSendAmendmentRequestAsAnswer { get; set; }
    
    public bool CanSendInvoiceAmendmentRequestAsAnswer { get; set; }
    
    public Sungero.Core.Enumeration? BuyerAcceptanceStatus { get; set; }
  }
  
  partial class DocumentCertificatesInfo
  {
    public List<ICertificate> Certificates { get; set; }
    
    public bool CanSign { get; set; }
    
    public List<ICertificate> MyCertificates { get; set; }
  }
  
  /// <summary>
  /// Информация об организации подписывающего.
  /// </summary>
  [Public]
  partial class OrganizationInfo
  {
    public string Name { get; set; }
    
    public string TIN { get; set; }
    
    public bool IsOurSignature { get; set; }
  }
  
  /// <summary>
  /// Информация о сторонах соглашения об аннулировании.
  /// </summary>
  [Public]
  partial class AnnulmentCounterpartyNames
  {
    public string SenderName { get; set; }
    
    public string ReceiverName { get; set; }
  }
  
  partial class Certificate
  {
    public string Thumbprint { get; set; }
    
    public IUser Owner { get; set; }
  }
  
  partial class FormalizedDocumentXML
  {
    public string DocumentNumber { get; set; }
    
    public string DocumentDate { get; set; }
    
    public bool IsAdjustment { get; set; }
    
    public string Comment { get; set; }
    
    public Docflow.IAccountingDocumentBase Corrected { get; set; }
    
    public Docflow.IContractualDocumentBase Contract { get; set; }
    
    [SuppressMessage("AppliedStylecopNamingRules.ApiNamingAnalyzer", "CR0001:ApiNamesMustNotContainCyrillic", Justification = "Deferred tech debt, #124193")]
    public Docflow.IAccountingDocumentBase СorrectionRevisionParentDocument { get; set; }
    
    public string CurrencyCode { get; set; }
    
    public double TotalAmount { get; set; }
    
    public bool IsRevision { get; set; }
    
    public Sungero.Core.Enumeration? Function { get; set; }
  }
  
  partial class Signature
  {
    public byte[] Body { get; set; }
    
    public int Id { get; set; }
    
    public string FormalizedPoAUnifiedRegNumber { get; set; }
  }
  
  [Public]
  partial class TaxDocumentClassifier
  {
    public string TaxDocumentClassifierCode { get; set; }
    
    public string TaxDocumentClassifierFunction { get; set; }
  }
}