using System;

namespace Sungero.Docflow.Constants
{
  public static class AccountingDocumentBase
  {
    // Выгрузка из архива.
    [Sungero.Core.Public]
    public const string ExportToFolder = "ExportToFolder";
    
    // Перевод Мб в байты и наоборот 1024*1024.
    public const int ConvertMb = 1048576;
    // Максимальное количество выгружаемых документов в веб.
    public const int ExportedDocumentsCountMaxLimit = 1000;
    // Максимальное количество файлов в выгружаемом zip архиве в веб.
    public const int ExportedFilesCountMaxLimit = 5000;
    // Максимальная сумма размеров файлов в выгружаемом zip архиве в веб.
    public const int ExportedFilesSizeMaxLimitMb = 450;

    // Максимальная длина должности для передачи через МКДО.
    [Sungero.Core.Public]
    public const int JobTitleMaxLength = 128;
    
    // Максимальная длина основания полномочий грузополучателя.
    [Sungero.Core.Public]
    public const int PowersBaseConsigneeMaxLength = 120;
    
    [Sungero.Core.Public]
    public const string IncomingTaxInvoiceGuid = "74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a";
    [Sungero.Core.Public]
    public const string OutcomingTaxInvoiceGuid = "f50c4d8a-56bc-43ef-bac3-856f57ca70be";
    public const string ContractStatementInvoiceGuid = "f2f5774d-5ca3-4725-b31d-ac618f6b8850";
    public const string WaybillInvoiceGuid = "4e81f9ca-b95a-4fd4-bf76-ea7176c215a7";
    
    [Sungero.Core.Public]
    public const string IncomingInvoiceGuid = "a523a263-bc00-40f9-810d-f582bae2205d";
    public const string OutgoingInvoiceGuid = "58ad01fb-6805-426b-9152-4de16d83b258";
    public const string UniversalTransferDocumentGuid = "58986e23-2b0a-4082-af37-bd1991bc6f7e";
    public static readonly Guid AccountingDocumentGuid = Guid.Parse("96c4f4f3-dc74-497a-b347-e8faf4afe320");
    public static readonly Guid FinancialArchiveUIGuid = Guid.Parse("e99ae7e2-edb7-4904-a19a-4577f07609a4");
    
    public static class HelpCodes
    {
      public const string Search = "Sungero_FinancialArchive_SearchDialog";
      public const string Export = "Sungero_FinancialArchive_ExportDialog";
      
      // ТОРГ-12.
      public const string Waybill = "Sungero_FinancialArchive_FillingDialog_Waybill";
      
      // ДПТ.
      public const string GoodsTransfer = "Sungero_FinancialArchive_FillingDialog_GoodsTransfer";
      
      // ДПТ от продавца.
      public const string SellerGoodsTransfer = "Sungero_FinancialArchive_Seller_FillingDialog_GoodsTransfer";
      
      // Акт.
      public const string ContractStatement = "Sungero_FinancialArchive_FillingDialog_ContractStatement";
      
      // ДПРР.
      public const string WorksTransfer = "Sungero_FinancialArchive_FillingDialog_WorksTransfer";
      
      // ДПРР от продавца.
      public const string SellerWorksTransfer = "Sungero_FinancialArchive_Seller_FillingDialog_WorksTransfer";
      
      // УПД.
      public const string UniversalTransfer = "Sungero_FinancialArchive_FillingDialog_UniversalTransfer";
      
      // УКД.
      public const string UniversalCorrectionTransfer = "Sungero_FinancialArchive_FillingDialog_UniversalCorrectionTransfer";

      // УПД от продавца.
      public const string SellerUniversalTransfer = "Sungero_FinancialArchive_Seller_FillingDialog_UniversalTransfer";
      
      // УКД от продавца.
      public const string SellerUniversalCorrectionTransfer = "Sungero_FinancialArchive_Seller_FillingDialog_UniversalCorrectionTransfer";
    }
    
    public static class GenerateTitleTypes
    {
      public const string Signatory = "Signatory";
      public const string Consignee = "Consignee";
      public const string ConsigneePowerOfAttorney = "ConsigneePowerOfAttorney";
      public const string SignatoryPowersBase = "SignatoryPowersBase";
    }
  }
}