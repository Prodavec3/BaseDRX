using System;
using Sungero.Core;

namespace Sungero.FinancialArchive.Constants
{
  public static class Module
  {
    [Sungero.Core.Public]
    public static readonly Guid FinancialArchiveResponsibleRole = Guid.Parse("A35F4302-848F-4022-84DD-4AA2978ECF9A");
    
    public static class HelpCodes
    {
      public const string Import = "Sungero_FinancialArchive_ImportDialog";
    }

    public static class SellerTitlePowers
    {
      // Лицо, совершившее сделку, операцию и ответственное за её оформление;
      public const string MadeAndSignOperation = "2";

      // лицо, ответственное за оформление свершившегося события;
      public const string PersonDocumentedOperation = "3";

      // лицо, совершившее сделку, операцию и ответственное за её оформление и за подписание счетов-фактур;
      public const string MadeAndResponsibleForOperationAndSignedInvoice = "5";
      
      // лицо, ответственное за оформление свершившегося события и за подписание счетов-фактур.
      public const string ResponsibleForOperationAndSignatoryForInvoice = "6";
    }
    
    public static class Initialize
    {
      [Sungero.Core.Public]
      public static readonly Guid WaybillDocumentKind = Guid.Parse("8DCF8D7A-9868-4620-83FF-8FF81A5BE520");
      [Sungero.Core.Public]
      public static readonly Guid IncomingTaxInvoiceKind = Guid.Parse("B873AF20-E9DC-419F-9090-D98BEB630B8D");
      [Sungero.Core.Public]
      public static readonly Guid OutgoingTaxInvoiceKind = Guid.Parse("D3EABD23-7B4D-401B-ACAD-D4142EAAF3BC");
      [Sungero.Core.Public]
      public static readonly Guid ContractStatementKind = Guid.Parse("C1F11157-4E42-4CE0-9423-AE05567B162E");
      [Sungero.Core.Public]
      public static readonly Guid UniversalTaxInvoiceAndBasicKind = Guid.Parse("CAC00143-A9A0-4EE1-98D1-F996A9F4CC5E");
      [Sungero.Core.Public]
      public static readonly Guid UniversalBasicKind = Guid.Parse("846B074A-2AF0-44B4-9611-ECC87EBE1A60");
      
      [Sungero.Core.Public]
      public static readonly Guid OutgoingTaxInvoiceRegister = Guid.Parse("CAEF4DA9-A844-4A81-A885-0C0D4F51ACA5");
      [Sungero.Core.Public]
      public static readonly Guid IncomingTaxInvoiceRegister = Guid.Parse("96320BC9-D54B-4979-B5E1-867462214EBE");
      [Sungero.Core.Public]
      public static readonly Guid WaybillRegister = Guid.Parse("5BD989EA-B4B0-429D-8186-8E3C24B3F273");
      [Sungero.Core.Public]
      public static readonly Guid ContractStatementRegister = Guid.Parse("D9B3A138-0507-4CF3-B3B2-A7B231666838");
      [Sungero.Core.Public]
      public static readonly Guid UniversalRegister = Guid.Parse("E4BE2FB7-66F3-4B1B-963C-ADAB5E5329AA");
      
      [Sungero.Core.Public]
      public static readonly Guid DocumentKindTypeGuid = Guid.Parse("14a59623-89a2-4ea8-b6e9-2ad4365f358c");
    }
  }
}