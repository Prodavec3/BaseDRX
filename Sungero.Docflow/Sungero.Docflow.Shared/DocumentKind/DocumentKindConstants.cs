using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class DocumentKind
  {
    public static readonly Guid WaybillDocumentKind = Guid.Parse("8DCF8D7A-9868-4620-83FF-8FF81A5BE520");
    public static readonly Guid IncomingTaxInvoiceKind = Guid.Parse("B873AF20-E9DC-419F-9090-D98BEB630B8D");
    public static readonly Guid OutgoingTaxInvoiceKind = Guid.Parse("D3EABD23-7B4D-401B-ACAD-D4142EAAF3BC");
    public static readonly Guid ContractStatementKind = Guid.Parse("C1F11157-4E42-4CE0-9423-AE05567B162E");
    public static readonly Guid UniversalTaxInvoiceAndBasicKind = Guid.Parse("CAC00143-A9A0-4EE1-98D1-F996A9F4CC5E");
    public static readonly Guid UniversalBasicKind = Guid.Parse("846B074A-2AF0-44B4-9611-ECC87EBE1A60");
    // Тип прав "Выбор из списка в документе".
    public static readonly Guid DocumentKindChoiseAccessRightType = Guid.Parse("f3c5d7c0-c5c3-4957-add6-193a2f44f267");
  }
}