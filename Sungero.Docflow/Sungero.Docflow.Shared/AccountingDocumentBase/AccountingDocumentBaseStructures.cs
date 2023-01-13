using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.AccountingDocumentBase
{
  [PublicAttribute]
  partial class BuyerTitle
  {
    public DateTime? AcceptanceDate { get; set; }
    
    /// <summary>
    /// Результат приемки.
    /// </summary>
    public Sungero.Core.Enumeration BuyerAcceptanceStatus { get; set; }
    
    /// <summary>
    /// Акт разногласий.
    /// </summary>
    public string ActOfDisagreement { get; set; }

    /// <summary>
    /// Подписывающий.
    /// </summary>
    public Company.IEmployee Signatory { get; set; }

    /// <summary>
    /// Основание полномочий подписывающего.
    /// </summary>
    public string SignatoryPowersBase { get; set; }

    /// <summary>
    /// Сотрудник, принявший груз.
    /// </summary>
    public Company.IEmployee Consignee { get; set; }

    /// <summary>
    /// Основание полномочий принявшего груз.
    /// </summary>
    public string ConsigneePowersBase { get; set; }
    
    /// <summary>
    /// Область полномочий (оформление, подписание, оформление и подписание).
    /// </summary>
    public string SignatoryPowers { get; set; }
    
    /// <summary>
    /// Доверенность.
    /// </summary>
    public IPowerOfAttorneyBase ConsigneePowerOfAttorney { get; set; }
    
    /// <summary>
    /// Другой документ.
    /// </summary>
    public string ConsigneeOtherReason { get; set; }
    
    /// <summary>
    /// Право подписи.
    /// </summary>
    public ISignatureSetting SignatureSetting { get; set; }
    
    /* TODO Ждёт баги 44693
    
    /// <summary>
    /// Доверенность подписывающего.
    /// </summary>
    public Sungero.Docflow.Structures.AccountingDocumentBase.IAttorney Attorney { get; set; }

    /// <summary>
    /// Доверенность принявшего груз.
    /// </summary>
    public Sungero.Docflow.Structures.AccountingDocumentBase.IAttorney ConsigneeAttorney { get; set; }
    
     */
  }
  
  [PublicAttribute]
  partial class SellerTitle
  {
    /// <summary>
    /// Подписывающий.
    /// </summary>
    public Company.IEmployee Signatory { get; set; }

    /// <summary>
    /// Основание полномочий подписывающего.
    /// </summary>
    public string SignatoryPowersBase { get; set; }
    
    /// <summary>
    /// Область полномочий (оформление, подписание, оформление и подписание).
    /// </summary>
    public string SignatoryPowers { get; set; }
    
    /// <summary>
    /// Право подписи.
    /// </summary>
    public ISignatureSetting SignatureSetting { get; set; }
  }
  
  partial class GenerateTitleError
  {
    public string Type { get; set; }
    
    public string Text { get; set; }
  }
}