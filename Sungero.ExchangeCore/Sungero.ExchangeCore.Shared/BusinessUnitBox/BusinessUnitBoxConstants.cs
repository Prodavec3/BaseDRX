using System;

namespace Sungero.ExchangeCore.Constants
{
  public static class BusinessUnitBox
  {
    public const string Delimiter = " - ";
    
    public const string Moscow = "г. Москва";
    public const string SaintPetersburg = "г. Санкт-Петербург";
    public const string Sevastopol = "г. Севастополь";
    public const string Baikonur = "г. Байконур";
    
    public const string House = "д.";
    public const string Building = "корп.";
    public const string Apartment = "кв.";
    
    public const string City = "г.";
    public const string Locality = "пгт.";
    public const int PasswordMaxLength = 50;
    
    public const int CounterpartySyncBatchSize = 25;
    
    /// <summary>
    /// Имя параметра "ИД сообщения" в строке ссылки на документ в системе обмена.
    /// </summary>
    public const string DocumentHyperlinkParameterLetterId = "letterId";
    
    /// <summary>
    /// Имя параметра "ИД документа" в строке ссылки на документ в системе обмена.
    /// </summary>
    public const string DocumentHyperlinkParameterDocumentId = "documentId";
    
    public const string ExchangeDocumentsSearchHelpCode = "Sungero_ExchangeCore_ExchangeDocumentsSearchDialog";
    
    public static readonly Guid ExchangeCoreDiadocGiud = Guid.Parse("30083842-5a15-4efb-9cab-0b61b1760157");
    public static readonly Guid ExchangeCoreSBISGiud = Guid.Parse("d764569f-fa35-48be-aec9-d337b185d47a");
  }
}