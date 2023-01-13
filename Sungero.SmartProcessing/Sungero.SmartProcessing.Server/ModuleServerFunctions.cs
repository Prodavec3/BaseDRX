using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommonLibrary;
using Sungero.Commons;
using Sungero.Commons.Structures.Module;
using Sungero.Company;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.Docflow.Structures.Module;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Parties;
using Sungero.RecordManagement;
using Sungero.SmartProcessing.Constants;
using Sungero.SmartProcessing.Structures.Module;
using Sungero.Workflow;
using ArioGrammars = Sungero.SmartProcessing.Constants.Module.ArioGrammars;
using ElasticsearchTypes = Sungero.Commons.PublicConstants.Module.ElasticsearchType;

namespace Sungero.SmartProcessing.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Обработать пакет бинарных образов документов DCS.
    /// </summary>
    /// <param name="packageId">Идентификатор пакета.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void ProcessCapturedPackage(string packageId)
    {
      this.LogMessage("Smart processing. Processing captured package...", packageId);

      var blobPackage = BlobPackages.GetAll().Where(x => x.PackageId == packageId).FirstOrDefault();
      
      var asyncProcessBlobPackageHandler = SmartProcessing.AsyncHandlers.ProcessBlobPackage.Create();
      asyncProcessBlobPackageHandler.BlobPackageId = blobPackage.Id;
      asyncProcessBlobPackageHandler.ExecuteAsync();
      
      this.LogMessage("Smart processing. Processing captured package completed successfully.", packageId);
    }
    
    /// <summary>
    /// Обработать пакет документов со сканера или почты.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    public virtual void ProcessCapturedPackage(IBlobPackage blobPackage)
    {
      var arioPackage = this.UnpackArioPackage(blobPackage);
      
      var documentPackage = this.BuildDocumentPackage(blobPackage, arioPackage);
      
      this.OrderAndLinkDocumentPackage(documentPackage);
      
      this.SendToResponsible(documentPackage);
      
      // Вызываем асинхронную выдачу прав, так как убрали ее при сохранении.
      this.EnqueueGrantAccessRightsJobs(documentPackage);

      this.FinalizeProcessing(blobPackage);
    }
    
    #region Обработка пакета документов через rxcmd
    
    /// <summary>
    /// Установка параметра AllIndicesExist в DocflowParams, при наличии всех индексов.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void UpdateDocflowParamsIfAllIndicesExist()
    {
      // Проверка существования индексов.
      var indicesNotExist = new List<string>();
      
      if (!Commons.PublicFunctions.Module.IsIndexExist(Commons.PublicFunctions.Module.GetIndexName(BusinessUnits.Info.Name)))
        indicesNotExist.Add(BusinessUnits.Info.Name);
      
      if (!Commons.PublicFunctions.Module.IsIndexExist(Commons.PublicFunctions.Module.GetIndexName(Employees.Info.Name)))
        indicesNotExist.Add(Employees.Info.Name);
      
      if (!Commons.PublicFunctions.Module.IsIndexExist(Commons.PublicFunctions.Module.GetIndexName(CompanyBases.Info.Name)))
        indicesNotExist.Add(CompanyBases.Info.Name);
      
      if (!Commons.PublicFunctions.Module.IsIndexExist(Commons.PublicFunctions.Module.GetIndexName(Contacts.Info.Name)))
        indicesNotExist.Add(Contacts.Info.Name);
      
      if (indicesNotExist.Any())
        Logger.ErrorFormat("SmartProcessing. UpdateDocflowParamsIfAllIndicesExist. Indices not created: {0}.", string.Join(", ", indicesNotExist));
      else
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Commons.PublicConstants.Module.AllIndicesExistParamName, string.Empty);
    }
    
    /// <summary>
    /// Удаление параметра AllIndicesExist из DocflowParams.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void RemoveAllIndicesExistFromDocflowParams()
    {
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.RemoveDocflowParamsValue, new[] { Commons.PublicConstants.Module.AllIndicesExistParamName });
    }
    
    /// <summary>
    /// Валидация настроек интеллектуальной обработки.
    /// </summary>
    /// <param name="senderLineName">Наименование линии.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void ValidateSettings(string senderLineName)
    {
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      Sungero.Docflow.PublicFunctions.SmartProcessingSetting.ValidateSettings(smartProcessingSettings, senderLineName);
    }
    
    /// <summary>
    /// Сформировать пакет бинарных образов документов на основе пакета документов из DCS.
    /// </summary>
    /// <param name="dcsPackage">Пакет документов из DCS.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void PrepareBlobPackage(Structures.Module.IDcsPackage dcsPackage)
    {
      dcsPackage.Blobs = this.ExcludeUnnecessaryDcsBlobs(dcsPackage.Blobs);
      this.CreateBlobPackage(dcsPackage);
    }

    /// <summary>
    /// Исключить лишние бинарные образы документов DCS из пакета, пришедшего с почты, которые не нужно заносить в систему.
    /// </summary>
    /// <param name="dcsBlobs">Бинарные образы документов DCS.</param>
    /// <returns>Актуальные бинарные образы документов DCS.</returns>
    [Public]
    public virtual List<Structures.Module.IDcsBlob> ExcludeUnnecessaryDcsBlobs(List<Structures.Module.IDcsBlob> dcsBlobs)
    {
      // Исключить из пакета тело письма (body.html или body.txt).
      var mailBodyDcsBlobs = this.GetMailBodyBlobs(dcsBlobs);
      if (mailBodyDcsBlobs != null)
      {
        foreach (var mailBodyDcsBlob in mailBodyDcsBlobs)
        {
          dcsBlobs = dcsBlobs
            .Where(b => !string.Equals(b.OriginalFileName, mailBodyDcsBlob.OriginalFileName, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
        }
      }
      
      // Исключить из пакета картинки тела письма.
      dcsBlobs = dcsBlobs.Where(b => !b.IsInlineMailContent).ToList();
      
      return dcsBlobs;
    }
    
    /// <summary>
    /// Получить тела письма из всех бинарных образов документов DCS.
    /// </summary>
    /// <param name="blobs">Бинарные образы документов DCS.</param>
    /// <returns>Тела письма.</returns>
    [Public]
    public virtual List<Structures.Module.IDcsBlob> GetMailBodyBlobs(List<Structures.Module.IDcsBlob> blobs)
    {
      var mailBodyHtmlName = ArioExtensions.DcsFileParser.Constants.DcsMailBodyName.Html;
      var mailBodyTxtName = ArioExtensions.DcsFileParser.Constants.DcsMailBodyName.Txt;

      var mailBodyBlobs = blobs.Where(b => string.Equals(b.OriginalFileName, mailBodyHtmlName, StringComparison.InvariantCultureIgnoreCase) ||
                                      string.Equals(b.OriginalFileName, mailBodyTxtName, StringComparison.InvariantCultureIgnoreCase)).ToList();
      return mailBodyBlobs;
    }
    
    /// <summary>
    /// Исключить из пакета бинарных образов документов DCS изображения, содержащиеся в html-теле письма.
    /// </summary>
    /// <param name="htmlMailBodyDcsBlob">Html-тело письма.</param>
    /// <param name="dcsBlobs">Бинарные образы документов DCS.</param>
    /// <returns>Бинарные образы документов DCS за исключением изображений в теле письма.</returns>
    [Public, Obsolete("Теперь картинки тела письма исключаются из пакета в ExcludeUnnecessaryDcsBlobs.")]
    public virtual List<Structures.Module.IDcsBlob> ExcludeEmailBodyInlineImages(Structures.Module.IDcsBlob htmlMailBodyDcsBlob,
                                                                                 List<Structures.Module.IDcsBlob> dcsBlobs)
    {
      // Метод находится здесь, потому что в нем есть привязка к AsposeExtensions.
      // ArioExtensions не ссылается на AsposeExtensions, поэтому метод перенести в ArioExtensions нельзя.
      // Получить количество ссылок на файлы-изображения в html-теле письма.
      var inlineImagesCount = AsposeExtensions.HtmlTagReader.GetInlineImagesCount(htmlMailBodyDcsBlob.Body);
      // Убрать из пакета файлы этих изображений (убираются первые n документов пакета, где n - количество картинок).
      return dcsBlobs.Skip(inlineImagesCount).ToList();
    }
    
    /// <summary>
    /// Создать пакет бинарных образов документов.
    /// </summary>
    /// <param name="dcsPackage">Пакет бинарных образов документов DCS.</param>
    /// <returns>Пакет бинарных образов документов.</returns>
    [Public]
    public virtual IBlobPackage CreateBlobPackage(Structures.Module.IDcsPackage dcsPackage)
    {
      // Заполнить основную информацию.
      var blobPackage = Functions.BlobPackage.CreateBlobPackage();
      blobPackage.SenderLine = dcsPackage.SenderLine;
      blobPackage.PackageId = dcsPackage.PackageId;
      if (dcsPackage.SourceType == ArioExtensions.DcsFileParser.Constants.CaptureSourceType.Mail)
      {
        blobPackage.SourceType = SmartProcessing.BlobPackage.SourceType.Mail;
        Functions.BlobPackage.FillMailInfoFromDcsPackage(blobPackage, dcsPackage);
      }
      else
      {
        blobPackage.SourceType = SmartProcessing.BlobPackage.SourceType.Folder;
      }
      blobPackage.PackageFolderPath = dcsPackage.PackageFolderPath;
      blobPackage.SourceName = dcsPackage.SourceName;
      blobPackage.DcsProcessingBeginDate = dcsPackage.DcsProcessingBeginDate.FromUtcTime();

      if (!dcsPackage.Blobs.Any() && blobPackage.SourceType == SmartProcessing.BlobPackage.SourceType.Folder)
        throw AppliedCodeException.Create(Resources.EmptyScanPackage);

      // Заполнить информацию о документах пакета.
      foreach (var dcsBlob in dcsPackage.Blobs)
      {
        var blob = Functions.Blob.CreateBlob(dcsBlob);
        blobPackage.Blobs.AddNew().Blob = blob;
      }
      
      // Заполнить тело письма.
      var mailBodyDcsBlob = dcsPackage.MailBodyBlob;
      if (mailBodyDcsBlob != null)
      {
        var bodyBase64 = Convert.ToBase64String(mailBodyDcsBlob.Body);
        if (!this.IsEmptyMailBody(bodyBase64, mailBodyDcsBlob.OriginalFileName))
          blobPackage.MailBodyBlob = Functions.Blob.CreateBlob(mailBodyDcsBlob);
      }
      blobPackage.Save();
      
      return blobPackage;
    }
    
    /// <summary>
    /// Проверить, пустое ли тело письма.
    /// </summary>
    /// <param name="mailBody">Тело письма, закодированное в Base64.</param>
    /// <param name="mailBodyFileName">Имя файла тела письма.</param>
    /// <returns>True - тело письма пустое, иначе - false.</returns>
    /// <remarks>Тело письма передается в формате Base64, так как в параметрах метода с атрибутами Remote и/или Public
    /// нельзя использовать сторонние библиотеки, а также массив байт.</remarks>
    [Remote(IsPure = true), Public]
    public virtual bool IsEmptyMailBody(string mailBody, string mailBodyFileName)
    {
      // Метод находится здесь, потому что в нем есть привязка к AsposeExtensions.
      // ArioExtensions не ссылается на AsposeExtensions, поэтому метод перенести в ArioExtensions нельзя.
      var mailBodyHtmlName = Constants.Module.DcsMailBodyName.Html;
      var mailBodyTxtName = Constants.Module.DcsMailBodyName.Txt;

      // Получить текст из тела письма.
      var bodyText = string.Empty;
      var convertedMailBody = Convert.FromBase64String(mailBody);
      if (string.Equals(mailBodyFileName, mailBodyHtmlName, StringComparison.InvariantCultureIgnoreCase))
        bodyText = AsposeExtensions.HtmlTagReader.GetTextFromHtml(convertedMailBody);
      else if (string.Equals(mailBodyFileName, mailBodyTxtName, StringComparison.InvariantCultureIgnoreCase))
      {
        using (var bodyStream = new MemoryStream(convertedMailBody))
          using (var streamReader = new StreamReader(bodyStream))
        {
          bodyText = streamReader.ReadToEnd();
        }
      }
      // Очистить текст из тела письма от спецсимволов, чтобы определить, пуст ли он.
      var clearBodyText = bodyText.Trim(new[] { ' ', '\r', '\n', '\0' });
      
      return string.IsNullOrWhiteSpace(clearBodyText);
    }
    
    /// <summary>
    /// Получить статус задачи на обработку файла.
    /// </summary>
    /// <param name="taskId">ИД задачи на обработку.</param>
    /// <returns>Статус и результат задачи.</returns>
    /// <remarks>Значения статусов Task.State: 0 - Новая, 1 - В работе, 2 - Завершена,
    /// 3 - Произошла ошибка, 4 - Обучение завершено, 5 - Прекращена.</remarks>
    public virtual ArioExtensions.Models.ProcessTaskInfo GetProcessTaskInfo(int taskId)
    {
      var arioConnector = this.GetArioConnector();
      return arioConnector.GetProcessTaskInfo(taskId);
    }
    
    /// <summary>
    /// Получить коннектор к Ario.
    /// </summary>
    /// <returns>Коннектор к Ario.</returns>
    /// <remarks> Функция по получению коннектора к Арио уже есть в Docflow,
    /// но ее нельзя использовать здесь, так как возвращаемый тип - Sungero.ArioExtensions.ArioConnector,
    /// а сторонние библиотеки не могут быть в качестве возвращаемого результата Public/Remote функций (ограничение платформы).
    /// Поэтому приходится дублировать функцию GetArioConnector в модуле SmartProcessing.</remarks>
    public virtual Sungero.ArioExtensions.ArioConnector GetArioConnector()
    {
      var timeoutInSeconds = Docflow.PublicFunctions.SmartProcessingSetting.Remote.GetArioConnectionTimeoutInSeconds();
      var timeout = new TimeSpan(0, 0, timeoutInSeconds);
      var smartProcessingSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var password = string.IsNullOrEmpty(smartProcessingSettings.Password)
        ? string.Empty
        : Encryption.Decrypt(smartProcessingSettings.Password);
      return ArioExtensions.ArioConnector.Get(smartProcessingSettings.ArioUrl, timeout, smartProcessingSettings.Login, password);
    }
    
    #endregion
    
    #region Распаковка и заполнение фактов
    
    /// <summary>
    /// Десериализовать результат классификации комплекта документов в Ario.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <returns>Десериализованный результат классификации комплекта документов в Ario.</returns>
    [Public]
    public virtual IArioPackage UnpackArioPackage(IBlobPackage blobPackage)
    {
      var packageId = blobPackage.PackageId;
      this.LogMessage("Smart processing. UnpackArioPackage...", packageId);
      
      var arioPackage = ArioPackage.Create();
      arioPackage.Documents = new List<IArioDocument>();
      if (blobPackage.Blobs.Count == 0)
        return arioPackage;
      
      var blobs = blobPackage.Blobs.Select(x => x.Blob);
      foreach (var blob in blobs)
      {
        var arioDocuments = this.UnpackArioDocuments(blob);
        arioPackage.Documents.AddRange(arioDocuments);
      }
      
      this.LogMessage("Smart processing. UnpackArioPackage.", packageId);
      return arioPackage;
    }
    
    /// <summary>
    /// Десериализовать результат классификации документа в Ario.
    /// </summary>
    /// <param name="blob">Бинарный образ документа.</param>
    /// <returns>Список документов, распознанных в Ario.</returns>
    [Public]
    public virtual List<IArioDocument> UnpackArioDocuments(IBlob blob)
    {
      var arioDocuments = new List<IArioDocument>();
      
      // Документ не был обработан в Ario.
      if (blob.ArioResultJson == null)
      {
        arioDocuments.Add(this.UnpackFailedProcessArioDocument(blob));
        return arioDocuments;
      }
      
      var packageProcessResults = ArioExtensions.ArioConnector.DeserializeClassifyAndExtractFactsResultString(blob.ArioResultJson);
      foreach (var packageProcessResult in packageProcessResults)
      {
        if (packageProcessResult.ClassificationResult == null)
          arioDocuments.Add(this.UnpackUnprocessedArioDocument(blob, packageProcessResult));
        else
          arioDocuments.Add(this.UnpackArioDocument(blob, packageProcessResult));
      }
      
      return arioDocuments;
    }
    
    /// <summary>
    /// Распаковать обработанный в Ario документ.
    /// </summary>
    /// <param name="blob">Бинарный образ документа.</param>
    /// <param name="packageProcessResult">Результат обработки.</param>
    /// <returns>Документ, обработанный в Ario.</returns>
    public virtual IArioDocument UnpackArioDocument(IBlob blob, ArioExtensions.Models.PackageProcessResult packageProcessResult)
    {
      var arioDocument = ArioDocument.Create();
      arioDocument.IsProcessedByArio = true;
      arioDocument.BodyFromArio = new byte[0];
      
      // Класс и гуид тела документа.
      var clsResult = packageProcessResult.ClassificationResult;
      arioDocument.BodyGuid = packageProcessResult.Guid;
      arioDocument.IsRecognized = clsResult.PredictedClass != null;
      arioDocument.OriginalBlob = blob;
      if (!arioDocument.IsRecognized)
      {
        var message = "Capture Service cannot classify the document.";
        if (blob != null)
          message = string.Format("{0} Original file name: {1}", message, blob.OriginalFileName);
        Logger.DebugFormat(message);
      }
      
      // Создать результат распознавания.
      var recognitionInfo = EntityRecognitionInfos.Create();
      recognitionInfo.RecognizedClass = arioDocument.IsRecognized ? clsResult.PredictedClass.Name : string.Empty;
      recognitionInfo.Name = recognitionInfo.RecognizedClass;
      if (clsResult.PredictedProbability != null)
        recognitionInfo.ClassProbability = (double)clsResult.PredictedProbability;
      
      // Доп. классификаторы.
      if (packageProcessResult.AdditionalClassificationResults != null)
      {
        foreach (var additionalClassificationResult in packageProcessResult.AdditionalClassificationResults.Results)
        {
          var additionalClassifier = recognitionInfo.AdditionalClassifiers.AddNew();
          additionalClassifier.ClassifierID = additionalClassificationResult.ClassifierId;
          var additionalPredictedClass = additionalClassificationResult.PredictedClass;
          additionalClassifier.PredictedClass = additionalPredictedClass != null ? additionalPredictedClass.Name : string.Empty;
          additionalClassifier.Probability = additionalClassificationResult.PredictedProbability;
        }
      }
      
      // Факты и поля фактов.
      this.FillAllFacts(packageProcessResult, arioDocument, recognitionInfo);
      recognitionInfo.Save();
      arioDocument.RecognitionInfo = recognitionInfo;
      
      // Печати и подписи.
      this.FillStamps(packageProcessResult, arioDocument);
      this.FillSignatures(packageProcessResult, arioDocument);
      
      return arioDocument;
    }
    
    /// <summary>
    /// Распаковать необработанный в Ario документ.
    /// </summary>
    /// <param name="blob">Бинарный образ документа.</param>
    /// <param name="packageProcessResult">Результат обработки.</param>
    /// <returns>Документ, необработанный в Ario.</returns>
    public virtual IArioDocument UnpackUnprocessedArioDocument(IBlob blob, ArioExtensions.Models.PackageProcessResult packageProcessResult)
    {
      var arioDocument = ArioDocument.Create();
      arioDocument.IsProcessedByArio = true;
      arioDocument.BodyFromArio = new byte[0];
      arioDocument.OriginalBlob = blob;
      arioDocument.IsProcessedByArio = true;
      arioDocument.IsRecognized = false;
      arioDocument.BodyGuid = packageProcessResult.Guid;
      
      var recognitionInfo = EntityRecognitionInfos.Create();
      recognitionInfo.RecognizedClass = string.Empty;
      recognitionInfo.Name = string.Empty;
      recognitionInfo.Save();
      arioDocument.RecognitionInfo = recognitionInfo;
      return arioDocument;
    }
    
    /// <summary>
    /// Распаковать документ обработанный в Ario с ошибкой.
    /// </summary>
    /// <param name="blob">Бинарный образ документа.</param>
    /// <returns>Документ, необработанный в Ario.</returns>
    public virtual IArioDocument UnpackFailedProcessArioDocument(IBlob blob)
    {
      var arioDocument = ArioDocument.Create();
      arioDocument.OriginalBlob = blob;
      arioDocument.IsProcessedByArio = false;
      arioDocument.IsRecognized = false;
      arioDocument.FailedArioProcessDocument = blob.ArioTaskStatus != SmartProcessing.Blob.ArioTaskStatus.Success && blob.ArioTaskId != null;
      arioDocument.BodyFromArio = new byte[0];
      return arioDocument;
    }
    
    /// <summary>
    /// Заполнить все факты и поля фактов.
    /// </summary>
    /// <param name="packageProcessResult">Результат обработки.</param>
    /// <param name="arioDocument">Распознанный в Ario документ.</param>
    /// <param name="docInfo">Справочник с результатами распознавания документа.</param>
    public virtual void FillAllFacts(ArioExtensions.Models.PackageProcessResult packageProcessResult,
                                     IArioDocument arioDocument,
                                     IEntityRecognitionInfo docInfo)
    {
      arioDocument.Facts = new List<IArioFact>();
      var smartProcessingSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (packageProcessResult.ExtractionResult.Facts != null)
      {
        var pages = packageProcessResult.ExtractionResult.DocumentPages;
        var facts = packageProcessResult.ExtractionResult.Facts
          .Where(f => !string.IsNullOrWhiteSpace(f.Name))
          .Where(f => f.Fields.Any())
          .ToList();
        foreach (var fact in facts)
        {
          var fields = fact.Fields.Where(f => f != null)
            .Where(f => f.Probability >= smartProcessingSettings.LowerConfidenceLimit)
            .Select(f => ArioFactField.Create(f.Id, f.Name, f.Value, f.Probability));
          arioDocument.Facts.Add(ArioFact.Create(fact.Id, fact.Name, fields.ToList()));
          
          foreach (var factField in fact.Fields)
          {
            var fieldInfo = docInfo.Facts.AddNew();
            fieldInfo.FactId = fact.Id;
            fieldInfo.FieldId = factField.Id;
            fieldInfo.FactName = fact.Name;
            fieldInfo.FieldName = factField.Name;
            fieldInfo.FieldProbability = factField.Probability;
            fieldInfo.FieldConfidence = factField.Сonfidence;
            var fieldValue = factField.Value;
            if (fieldValue != null && fieldValue.Length > 1000)
            {
              fieldValue = fieldValue.Substring(0, 1000);
              Logger.DebugFormat("WARN. Value truncated. Length is over 1000 characters. GetRecognitionResults. FactID({0}). FieldID({1}).",
                                 fact.Id,
                                 factField.Id);
            }
            fieldInfo.FieldValue = fieldValue;
            
            // Позиция подсветки фактов в теле документа.
            if (factField.Positions != null)
            {
              var positions = factField.Positions
                .Where(p => p != null)
                .Select(p => this.CalculatePosition(p, pages));
              fieldInfo.Position = string.Join(Docflow.PublicConstants.Module.PositionsDelimiter.ToString(), positions);
            }
          }
        }
      }
    }
    
    /// <summary>
    /// Заполнить информацию о печатях.
    /// </summary>
    /// <param name="packageProcessResult">Результат обработки.</param>
    /// <param name="arioDocument">Распознанный в Ario документ.</param>
    public virtual void FillStamps(ArioExtensions.Models.PackageProcessResult packageProcessResult,
                                   IArioDocument arioDocument)
    {
      arioDocument.Stamps = new List<IArioStamp>();
      if (packageProcessResult.ExtractionResult == null)
        return;
      
      if (packageProcessResult.ExtractionResult.Stamps != null)
      {
        var pages = packageProcessResult.ExtractionResult.DocumentPages;
        foreach (var stampInfo in packageProcessResult.ExtractionResult.Stamps)
        {
          var stamp = ArioStamp.Create();
          
          stamp.Probability = stampInfo.Probability;
          stamp.Angle = stampInfo.Angle;
          var position = this.CalculatePosition(stampInfo.Position, pages);
          stamp.Position = string.Join(Docflow.PublicConstants.Module.PositionsDelimiter.ToString(), position);
          arioDocument.Stamps.Add(stamp);
        }
      }
    }
    
    /// <summary>
    /// Заполнить информацию о подписях.
    /// </summary>
    /// <param name="packageProcessResult">Результат обработки.</param>
    /// <param name="arioDocument">Распознанный в Ario документ.</param>
    public virtual void FillSignatures(ArioExtensions.Models.PackageProcessResult packageProcessResult,
                                       IArioDocument arioDocument)
    {
      arioDocument.Signatures = new List<IArioSignature>();
      if (packageProcessResult.ExtractionResult == null)
        return;
      
      if (packageProcessResult.ExtractionResult.Signatures != null)
      {
        var pages = packageProcessResult.ExtractionResult.DocumentPages;
        foreach (var signatureInfo in packageProcessResult.ExtractionResult.Signatures)
        {
          var signature = ArioSignature.Create();
          
          signature.Probability = signatureInfo.Probability;
          signature.Angle = signatureInfo.Angle;
          var position = this.CalculatePosition(signatureInfo.Position, pages);
          signature.Position = string.Join(Docflow.PublicConstants.Module.PositionsDelimiter.ToString(), position);
          arioDocument.Signatures.Add(signature);
        }
      }
    }
    
    /// <summary>
    /// Вычислить строку с информацией о позиции в документе.
    /// </summary>
    /// <param name="position">Позиция в документе.</param>
    /// <param name="pages">Информация о страницах.</param>
    /// <returns>Строка с информацией о позицией в документе.</returns>
    public virtual string CalculatePosition(ArioExtensions.Models.Position position, List<ArioExtensions.Models.PageInfo> pages)
    {
      return string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}",
                           Docflow.PublicConstants.Module.PositionElementDelimiter,
                           position.Page,
                           (int)Math.Round(position.Top),
                           (int)Math.Round(position.Left),
                           (int)Math.Round(position.Width),
                           (int)Math.Round(position.Height),
                           (int)Math.Round(pages.Where(x => x.Number == position.Page).Select(x => x.Width).FirstOrDefault()),
                           (int)Math.Round(pages.Where(x => x.Number == position.Page).Select(x => x.Height).FirstOrDefault()));
    }
    
    #endregion
    
    #region Создание и заполнение пакета
    
    /// <summary>
    /// Сформировать пакет документов.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <param name="arioPackage">Пакет результатов обработки документов в Ario.</param>
    /// <returns>Пакет созданных документов.</returns>
    [Public]
    public virtual IDocumentPackage BuildDocumentPackage(IBlobPackage blobPackage, IArioPackage arioPackage)
    {
      var packageId = blobPackage.PackageId;
      this.LogMessage("Smart processing. BuildDocumentPackage...", packageId);
      
      var documentPackage = this.PrepareDocumentPackage(blobPackage, arioPackage);
      
      documentPackage.Responsible = this.GetResponsible(blobPackage);
      
      foreach (var documentInfo in documentPackage.DocumentInfos)
      {
        try
        {
          var document = this.CreateDocument(documentInfo, documentPackage);
          this.CreateVersion(document, documentInfo);
          
          if (!documentInfo.FailedCreateVersion)
          {
            this.FillDeliveryMethod(document, blobPackage.SourceType);
            this.FillVerificationState(document);
          }
          
          this.SaveDocument(document, documentInfo);
        }
        catch (Exception ex)
        {
          this.LogError("Smart processing. BuildDocumentPackage. Error while trying to create the document.", ex, packageId);
          documentInfo.FailedCreateDocument = true;
          this.CreateSimpleDocument(documentInfo, documentPackage.Responsible, blobPackage.SourceType);
        }
      }
      this.CreateDocumentFromEmailBody(documentPackage);
      
      this.LogMessage("Smart processing. BuildDocumentPackage.", packageId);
      return documentPackage;
    }
    
    /// <summary>
    /// Создать незаполненный пакет документов.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <param name="arioPackage">Пакет результатов обработки документов в Ario.</param>
    /// <returns>Заготовка пакета документов.</returns>
    [Public]
    public virtual IDocumentPackage PrepareDocumentPackage(IBlobPackage blobPackage, IArioPackage arioPackage)
    {
      var isFuzzySearchEnabled = this.IsFuzzySearchEnabled();
      if (!isFuzzySearchEnabled)
        this.LogMessage("Smart processing. PrepareDocumentPackage. Fuzzy search does not enabled.", blobPackage);
      
      var documentInfos = new List<IDocumentInfo>();
      foreach (var arioDocument in arioPackage.Documents)
      {
        try
        {
          this.FillArioDocumentBody(arioDocument);
        }
        catch (Exception ex)
        {
          arioDocument.IsProcessedByArio = false;
          arioDocument.IsRecognized = false;
          arioDocument.FailedArioProcessDocument = true;
          
          if (blobPackage.SourceType != SmartProcessing.BlobPackage.SourceType.Mail)
            throw ex;
          
          this.LogError("Smart processing. PrepareDocumentPackage. Error while filling Ario document body.", ex, blobPackage);
        }
        var documentInfo = this.CreateDocumentInfo(arioDocument);
        documentInfo.IsFuzzySearchEnabled = isFuzzySearchEnabled;
        documentInfos.Add(documentInfo);
      }

      var documentPackage = DocumentPackage.Create();
      documentPackage.DocumentInfos = documentInfos;
      documentPackage.BlobPackage = blobPackage;

      return documentPackage;
    }
    
    /// <summary>
    /// Заполнить тело документа, полученного из Ario.
    /// </summary>
    /// <param name="arioDocument">Распознанный в Ario документ.</param>
    [Public]
    public virtual void FillArioDocumentBody(IArioDocument arioDocument)
    {
      arioDocument.BodyFromArio = new byte[0];
      
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (arioDocument.IsProcessedByArio)
      {
        using (var bodyFromArio = Sungero.Docflow.PublicFunctions.SmartProcessingSetting
               .GetDocumentBody(smartProcessingSettings, arioDocument.BodyGuid))
        {
          var bufferLen = (int)bodyFromArio.Length;
          var buffer = new byte[bufferLen];
          bodyFromArio.Read(buffer, 0, bufferLen);
          arioDocument.BodyFromArio = buffer;
        }
      }
    }
    
    /// <summary>
    /// Создать информацию о документе.
    /// </summary>
    /// <param name="arioDocument">Распознанный в Ario документ.</param>
    /// <returns>Информация о документе.</returns>
    [Public]
    public virtual IDocumentInfo CreateDocumentInfo(IArioDocument arioDocument)
    {
      var documentInfo = new DocumentInfo();
      documentInfo.ArioDocument = arioDocument;
      documentInfo.IsRecognized = arioDocument.IsRecognized;
      documentInfo.FailedArioProcessDocument = arioDocument.FailedArioProcessDocument;
      
      return documentInfo;
    }
    
    /// <summary>
    /// Получить ответственного за верификацию пакета документов.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <returns>Ответственный за верификацию пакета документов.</returns>
    [Public]
    public IEmployee GetResponsible(IBlobPackage blobPackage)
    {
      var smartProcessingSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var responsible = Docflow.PublicFunctions.SmartProcessingSetting
        .GetDocumentProcessingResponsible(smartProcessingSettings, blobPackage.SenderLine);
      
      return responsible;
    }
    
    /// <summary>
    /// Создать простой документ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию пакета документов.</param>
    /// <param name="sourceType">Тип источника.</param>
    [Public]
    public virtual void CreateSimpleDocument(IDocumentInfo documentInfo, IEmployee responsible, Enumeration? sourceType)
    {
      var document = this.CreateSimpleDocument(documentInfo, responsible);
      this.CreateVersion(document, documentInfo);
      
      if (!documentInfo.FailedCreateVersion)
      {
        this.FillDeliveryMethod(document, sourceType);
        this.FillVerificationState(document);
      }
      
      this.SaveDocument(document, documentInfo);
      documentInfo.Document = document;
    }
    
    #endregion
    
    #region Создание документов
    
    #region Общий механизм создания документов
    
    /// <summary>
    /// Создать документ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="documentPackage">Пакет документов.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public virtual IOfficialDocument CreateDocument(IDocumentInfo documentInfo, IDocumentPackage documentPackage)
    {
      var document = OfficialDocuments.Null;
      var arioDocument = documentInfo.ArioDocument;
      if (!arioDocument.FailedArioProcessDocument)
      {
        var predictedClass = documentInfo.IsRecognized ? arioDocument.RecognitionInfo.RecognizedClass : string.Empty;
        document = arioDocument.IsProcessedByArio ? this.GetDocumentByBarcode(documentInfo) : OfficialDocuments.Null;
        if (document == null)
          document = this.CreateDocumentByFacts(predictedClass, documentInfo, documentPackage.Responsible);
      }
      else
      {
        document = this.CreateSimpleDocument(documentInfo, documentPackage.Responsible);
      }
      documentInfo.Document = document;

      return document;
    }
    
    /// <summary>
    /// Создать документ по классу и фактам.
    /// </summary>
    /// <param name="className">Имя класса.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Документ.</returns>
    [Public]
    public virtual IOfficialDocument CreateDocumentByFacts(string className,
                                                           IDocumentInfo documentInfo,
                                                           IEmployee responsible)
    {
      // Если не нашли правило для обработки по имени класса или класс не распознался,
      // то взять правило с пустым именем класса.
      var smartProcessingSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var processingRule = smartProcessingSettings.ProcessingRules
        .Where(x => string.Equals(x.ClassName, className, StringComparison.InvariantCultureIgnoreCase))
        .FirstOrDefault();
      
      if (processingRule == null)
      {
        if (!string.IsNullOrEmpty(className))
          Logger.DebugFormat("Smart processing. There is no processing rule for class '{0}'", className);
        processingRule = smartProcessingSettings.ProcessingRules
          .Where(x => string.IsNullOrWhiteSpace(x.ClassName))
          .FirstOrDefault();
      }
      
      var document = OfficialDocuments.Null;
      var parameters = new object[] { documentInfo, responsible };
      if (processingRule != null)
        document = (IOfficialDocument)ExecuteModuleServerFunction(processingRule.ModuleName, processingRule.FunctionName, parameters);
      
      return document;
    }
    
    /// <summary>
    /// Создать тело документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void CreateVersion(IOfficialDocument document,
                                      IDocumentInfo documentInfo)
    {
      var versionNote = string.Empty;
      if (documentInfo.FoundByBarcode)
      {
        var documentParams = ((Domain.Shared.IExtendedEntity)document).Params;
        var documentLockInfo = Locks.GetLockInfo(document);
        if (documentLockInfo.IsLocked)
        {
          documentInfo.FailedCreateVersion = true;
          return;
        }
        else
        {
          documentParams[Docflow.PublicConstants.OfficialDocument.FindByBarcodeParamName] = true;
          versionNote = OfficialDocuments.Resources.VersionCreatedByCaptureService;
        }
      }
      
      this.CreateVersion(document, documentInfo.ArioDocument, versionNote);
    }
    
    /// <summary>
    /// Создать тело документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="arioDocument">Результат обработки входящего документа в Арио.</param>
    /// <param name="versionNote">Примечание к версии.</param>
    [Public]
    public virtual void CreateVersion(IOfficialDocument document,
                                      IArioDocument arioDocument,
                                      string versionNote = "")
    {
      var needCreatePublicBody = arioDocument.OriginalBlob != null && arioDocument.OriginalBlob.Body.Size != 0;
      var isRecognized = arioDocument.RecognitionInfo != null && !arioDocument.FailedArioProcessDocument;
      var pdfApp = Content.AssociatedApplications.GetByExtension(Docflow.PublicConstants.OfficialDocument.PdfExtension);
      if (pdfApp == Content.AssociatedApplications.Null)
        pdfApp = Docflow.PublicFunctions.Module.GetAssociatedApplicationByFileName(arioDocument.OriginalBlob.FilePath);
      
      var originalFileApp = Content.AssociatedApplications.Null;
      if (needCreatePublicBody || !isRecognized)
        originalFileApp = Docflow.PublicFunctions.Module.GetAssociatedApplicationByFileName(arioDocument.OriginalBlob.FilePath);
      
      // При создании версии Subject не должен быть пустым, иначе задваивается имя документа.
      var subjectIsEmpty = string.IsNullOrEmpty(document.Subject);
      if (subjectIsEmpty)
        document.Subject = "tmp_Subject";
      
      document.CreateVersion();
      var version = document.LastVersion;
      
      if (!isRecognized)
      {
        using (var body = arioDocument.OriginalBlob.Body.Read())
          version.Body.Write(body);
        version.AssociatedApplication = originalFileApp;
      }
      else if (needCreatePublicBody)
      {
        using (var publicBody = new MemoryStream(arioDocument.BodyFromArio))
          version.PublicBody.Write(publicBody);
        using (var body = arioDocument.OriginalBlob.Body.Read())
          version.Body.Write(body);
        version.AssociatedApplication = pdfApp;
        version.BodyAssociatedApplication = originalFileApp;
      }
      else
      {
        using (var body = new MemoryStream(arioDocument.BodyFromArio))
          version.Body.Write(body);
        
        version.AssociatedApplication = pdfApp;
      }
      
      if (!string.IsNullOrEmpty(versionNote))
        version.Note = versionNote;
      
      // Очистить Subject, если он был пуст до создания версии.
      if (subjectIsEmpty)
        document.Subject = string.Empty;
    }
    
    /// <summary>
    /// Сохранить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void SaveDocument(IOfficialDocument document, IDocumentInfo documentInfo)
    {
      if (!documentInfo.FailedCreateVersion)
      {
        // Удаляем параметр, чтобы не вызывать асинхронный обработчик по выдаче прав на документ, так как это вызывает ошибку (Bug 199971, 202010).
        // Асинхронный обработчик запускается после выполнения всех операций по документу.
        var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)document).Params;
        if (documentParams.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync))
          documentParams.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync);
        document.Save();
      }

      var arioDocument = documentInfo.ArioDocument;
      if (arioDocument.IsProcessedByArio)
      {
        // Добавить ИД документа в запись справочника с результатами обработки Ario.
        arioDocument.RecognitionInfo.EntityId = document.Id;
        // Заполнить поле Тип сущности guid'ом конечного типа сущности.
        arioDocument.RecognitionInfo.EntityType = document.GetEntityMetadata().GetOriginal().NameGuid.ToString();
        arioDocument.RecognitionInfo.Save();
      }
    }
    
    /// <summary>
    /// Выполнить серверную функцию модуля.
    /// </summary>
    /// <param name="moduleName">Имя решения и модуля.</param>
    /// <param name="functionName">Имя функции.</param>
    /// <param name="parameters">Массив параметров.</param>
    /// <returns>Результат выполнения.</returns>
    /// <remarks>Нельзя исп. params в public-функциях.</remarks>
    [Public]
    public static object ExecuteModuleServerFunction(string moduleName, string functionName, object[] parameters)
    {
      var functionTypeName = string.Format("{0}.Functions.Module", moduleName);
      var sharedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains(string.Format("{0}.Server", moduleName)));
      if (sharedAssemblies.Count() == 0)
      {
        throw new Exception(string.Format("Smart processing. Module \"{0}\" does not exist.", moduleName));
      }
      var sharedAssembly = sharedAssemblies.First();
      var modulesFunctions = sharedAssembly.GetTypes().First(a => a.FullName == functionTypeName);
      
      var method = modulesFunctions.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
        .Where(x => x.Name == functionName && x.GetParameters().Length == parameters.Length).SingleOrDefault();
      if (method == null)
      {
        throw new Exception(string.Format("Smart processing. Function \"{0}\" of module \"{1}\" does not exist.", functionName, moduleName));
      }
      var document = method.Invoke(null, parameters);

      return document;
    }

    /// <summary>
    /// Проверить возможность использования нечеткого поиска при заполнении карточек документов.
    /// </summary>
    /// <returns>True - если нечеткий поиск включен, иначе - false.</returns>
    public virtual bool IsFuzzySearchEnabled()
    {
      return Commons.PublicFunctions.Module.IsIntelligenceEnabled() &&
        Commons.PublicFunctions.Module.IsElasticsearchEnabled() &&
        Commons.PublicFunctions.Module.IsElasticsearchConfigured();
    }

    #endregion
    
    #region Создание конкретных типов документов
    
    /// <summary>
    /// Создать входящее письмо.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Входящее письмо.</returns>
    [Public]
    public virtual IOfficialDocument CreateIncomingLetter(IDocumentInfo documentInfo,
                                                          IEmployee responsible)
    {
      // Входящее письмо.
      var document = RecordManagement.IncomingLetters.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillIncomingLetterPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillIncomingLetterProperties(document, documentInfo, responsible);
      
      return document;
    }
    
    /// <summary>
    /// Создать акт выполненных работ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Акт выполненных работ.</returns>
    [Public]
    public virtual IOfficialDocument CreateContractStatement(IDocumentInfo documentInfo, IEmployee responsible)
    {
      // Акт выполненных работ.
      var document = FinancialArchive.ContractStatements.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillContractStatementPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillContractStatementProperties(document, documentInfo, responsible);
      
      return document;
    }
    
    /// <summary>
    /// Создать товарную накладную.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Товарная накладная.</returns>
    [Public]
    public virtual IOfficialDocument CreateWaybill(IDocumentInfo documentInfo, IEmployee responsible)
    {
      // Товарная накладная.
      var document = FinancialArchive.Waybills.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillWaybillPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillWaybillProperties(document, documentInfo, responsible);
      
      return document;
    }
    
    /// <summary>
    /// Создать счет-фактуру.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Счет-фактура.</returns>
    [Public]
    public virtual IOfficialDocument CreateTaxInvoice(IDocumentInfo documentInfo,
                                                      IEmployee responsible)
    {
      var documentParties = documentInfo.IsFuzzySearchEnabled ?
        this.GetRecognizedTaxInvoicePartiesFuzzy(documentInfo.ArioDocument.Facts, responsible) :
        this.GetRecognizedTaxInvoiceParties(documentInfo.ArioDocument.Facts, responsible);
      
      if (documentParties.IsDocumentOutgoing.Value == true)
      {
        var document = FinancialArchive.OutgoingTaxInvoices.Create();
        this.FillOutgoingTaxInvoiceProperties(document, documentInfo, responsible, documentParties);
        return document;
      }
      else
      {
        var document = FinancialArchive.IncomingTaxInvoices.Create();
        this.FillIncomingTaxInvoiceProperties(document, documentInfo, responsible, documentParties);
        return document;
      }
    }
    
    /// <summary>
    /// Создать корректировочный счет-фактуру.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Корректировочный счет-фактура.</returns>
    [Public]
    public virtual IOfficialDocument CreateTaxInvoiceCorrection(IDocumentInfo documentInfo,
                                                                IEmployee responsible)
    {
      var documentParties = documentInfo.IsFuzzySearchEnabled ?
        this.GetRecognizedTaxInvoicePartiesFuzzy(documentInfo.ArioDocument.Facts, responsible) :
        this.GetRecognizedTaxInvoiceParties(documentInfo.ArioDocument.Facts, responsible);
      
      if (documentParties.IsDocumentOutgoing.Value == true)
      {
        var document = FinancialArchive.OutgoingTaxInvoices.Create();
        document.IsAdjustment = true;
        this.FillOutgoingTaxInvoiceProperties(document, documentInfo, responsible, documentParties);
        return document;
      }
      else
      {
        var document = FinancialArchive.IncomingTaxInvoices.Create();
        document.IsAdjustment = true;
        this.FillIncomingTaxInvoiceProperties(document, documentInfo, responsible, documentParties);
        return document;
      }
    }
    
    /// <summary>
    /// Создать универсальный передаточный документ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Универсальный передаточный документ.</returns>
    [Public]
    public virtual IOfficialDocument CreateUniversalTransferDocument(IDocumentInfo documentInfo,
                                                                     IEmployee responsible)
    {
      // УПД.
      var document = FinancialArchive.UniversalTransferDocuments.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillUniversalTransferDocumentPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillUniversalTransferDocumentProperties(document, documentInfo, responsible);
      return document;
    }
    
    /// <summary>
    /// Создать универсальный корректировочный документ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Универсальный корректировочный документ.</returns>
    [Public]
    public virtual IOfficialDocument CreateUniversalTransferCorrectionDocument(IDocumentInfo documentInfo,
                                                                               IEmployee responsible)
    {
      // УКД.
      var document = FinancialArchive.UniversalTransferDocuments.Create();
      document.IsAdjustment = true;
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillUniversalTransferDocumentPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillUniversalTransferDocumentProperties(document, documentInfo, responsible);
      return document;
    }
    
    /// <summary>
    /// Создать входящий счет на оплату.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Входящий счет на оплату.</returns>
    [Public]
    public virtual IOfficialDocument CreateIncomingInvoice(IDocumentInfo documentInfo,
                                                           IEmployee responsible)
    {
      // Счет на оплату.
      var document = Contracts.IncomingInvoices.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillIncomingInvoicePropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillIncomingInvoiceProperties(document, documentInfo, responsible);
      return document;
    }
    
    /// <summary>
    /// Создать договор.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Договор.</returns>
    [Public]
    public virtual IOfficialDocument CreateContract(IDocumentInfo documentInfo,
                                                    IEmployee responsible)
    {
      // Договор.
      var document = Contracts.Contracts.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillContractPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillContractProperties(document, documentInfo, responsible);
      
      return document;
    }
    
    /// <summary>
    /// Создать доп. соглашение.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Доп. соглашение.</returns>
    [Public]
    public virtual IOfficialDocument CreateSupAgreement(IDocumentInfo documentInfo,
                                                        IEmployee responsible)
    {
      // Доп.соглашение.
      var document = Contracts.SupAgreements.Create();
      if (documentInfo.IsFuzzySearchEnabled)
        this.FillSupAgreementPropertiesFuzzy(document, documentInfo, responsible);
      else
        this.FillSupAgreementProperties(document, documentInfo, responsible);
      
      return document;
    }
    
    /// <summary>
    /// Создать простой документ.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>Простой документ.</returns>
    [Public]
    public virtual IOfficialDocument CreateSimpleDocument(IDocumentInfo documentInfo,
                                                          IEmployee responsible)
    {
      // Все нераспознанные документы создать простыми.
      var document = Docflow.SimpleDocuments.Create();
      
      // Имя документа сделать шаблонным, чтобы не падало сохранение, т.к. это свойство обязательное у документа.
      // Заполнение нужным значением будет выполнено в RenameNotClassifiedDocuments.
      var documentName = Resources.SimpleDocumentName;
      
      this.FillSimpleDocumentProperties(document, documentInfo, responsible, documentName);
      
      return document;
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение свойств документов
    
    #region Заполнение общих свойств
    
    /// <summary>
    /// Заполнить способ доставки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sourceType">Тип источника.</param>
    [Public]
    public virtual void FillDeliveryMethod(IOfficialDocument document, Sungero.Core.Enumeration? sourceType)
    {
      var methodName = sourceType == SmartProcessing.BlobPackage.SourceType.Folder
        ? MailDeliveryMethods.Resources.MailMethod
        : MailDeliveryMethods.Resources.EmailMethod;
      
      document.DeliveryMethod = MailDeliveryMethods.GetAll()
        .Where(m => m.Name.Equals(methodName, StringComparison.InvariantCultureIgnoreCase))
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Заполнить статус верификации для документов, в которых поддерживается режим верификации.
    /// </summary>
    /// <param name="document">Документ.</param>
    [Public]
    public virtual void FillVerificationState(IOfficialDocument document)
    {
      document.VerificationState = Docflow.OfficialDocument.VerificationState.InProcess;
    }
    
    /// <summary>
    /// Заполнить вид документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <remarks>Заполняется видом документа по умолчанию.
    /// Если вид документа по умолчанию не указан, то формируется список всех доступных видов документа
    /// и берется первый элемент из этого списка.</remarks>
    [Public]
    public virtual void FillDocumentKind(Docflow.IOfficialDocument document)
    {
      var documentKind = Docflow.PublicFunctions.OfficialDocument.GetDefaultDocumentKind(document);
      if (documentKind == null)
      {
        documentKind = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(document).FirstOrDefault();
        if (documentKind == null)
        {
          Logger.Error(string.Format("Cannot fill document kind for document type {0}.", Commons.PublicFunctions.Module.GetFinalTypeName(document)));
          return;
        }
        Logger.Debug(string.Format("Cannot find default document kind for document type {0}", Commons.PublicFunctions.Module.GetFinalTypeName(document)));
      }
      document.DocumentKind = documentKind;
    }
    
    #endregion
    
    #region Заполнение регистрационных данных
    
    /// <summary>
    /// Заполнить регистрационные данные документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="factName">Наименование факта с датой и номером документа.</param>
    /// <param name="withoutNumberLabel">Замещающий текст для номера, если он не распознан или отсутствует.</param>
    /// <remarks>Нумеруемым типам присваиваем номер, у остальных просто заполняем поля.</remarks>
    [Public]
    public virtual void FillDocumentRegistrationData(IOfficialDocument document,
                                                     IDocumentInfo documentInfo,
                                                     string factName,
                                                     string withoutNumberLabel)
    {
      // Для ненумеруемых документов регистрации нет.
      if (document.DocumentKind == null || document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
        return;
      
      if (document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
        this.NumberDocument(document, documentInfo, factName, withoutNumberLabel);
      
      // Если не получилось пронумеровать, то заполнить дату и номер по логике регистрируемых документов.
      if (document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable || documentInfo.RegistrationFailed)
        this.FillDocumentNumberAndDate(document, documentInfo, factName, withoutNumberLabel);
    }
    
    /// <summary>
    /// Пронумеровать документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="factName">Наименование факта с датой и номером документа.</param>
    /// <param name="withoutNumberLabel">Замещающий текст для номера, если он не распознан или отсутствует. По умолчанию "???".</param>
    [Public]
    public virtual void NumberDocument(Docflow.IOfficialDocument document,
                                       IDocumentInfo documentInfo,
                                       string factName,
                                       string withoutNumberLabel)
    {
      // Присвоить номер, если вид документа - нумеруемый.
      if (document.DocumentKind == null || document.DocumentKind.NumberingType != Docflow.DocumentKind.NumberingType.Numerable)
        return;
      
      var arioDocument = documentInfo.ArioDocument;
      
      // Проверить конфигурацию DirectumRX на возможность нумерации документа.
      // Можем нумеровать только тогда, когда однозначно подобран журнал.
      var registersIds = Docflow.PublicFunctions.OfficialDocument.GetDocumentRegistersIdsByDocument(document, Docflow.RegistrationSetting.SettingType.Numeration);
      
      // Если не смогли пронумеровать, то передать параметр с результатом в задачу на обработку документа.
      if (registersIds.Count != 1)
      {
        documentInfo.RegistrationFailed = true;
        return;
      }
      var register = DocumentRegisters.Get(registersIds.First());

      var props = document.Info.Properties;
      
      // Дата.
      var recognizedDate = this.GetRecognizedDate(arioDocument.Facts, factName, ArioGrammars.DocumentFact.DateField);
      // Если дата не распозналась или меньше минимальной, то подставить минимальную дату с минимальной вероятностью.
      if (!recognizedDate.Date.HasValue || recognizedDate.Date < Calendar.SqlMinValue)
      {
        recognizedDate.Date = Calendar.SqlMinValue;
        recognizedDate.Probability = Module.PropertyProbabilityLevels.Min;
      }
      
      // Номер.
      var recognizedNumber = this.GetRecognizedNumber(arioDocument.Facts, factName, ArioGrammars.DocumentFact.NumberField,
                                                      props.RegistrationNumber);
      // Если номер не распознался, то подставить заданное значение с минимальной вероятностью.
      if (string.IsNullOrWhiteSpace(recognizedNumber.Number))
      {
        recognizedNumber.Number = string.IsNullOrEmpty(withoutNumberLabel) ? Docflow.Resources.UnknownNumber : withoutNumberLabel;
        recognizedNumber.Probability = Module.PropertyProbabilityLevels.Min;
      }
      
      // Не нумеровать, если номер не уникален.
      if (recognizedDate.Date.HasValue)
      {
        var depCode = document.Department != null ? document.Department.Code : string.Empty;
        var bunitCode = document.BusinessUnit != null ? document.BusinessUnit.Code : string.Empty;
        var caseIndex = document.CaseFile != null ? document.CaseFile.Index : string.Empty;
        var kindCode = document.DocumentKind != null ? document.DocumentKind.Code : string.Empty;
        var counterpartyCode = Docflow.PublicFunctions.OfficialDocument.GetCounterpartyCode(document);
        var leadingDocumentId = document.LeadingDocument != null ? document.LeadingDocument.Id : 0;
        if (!Docflow.PublicFunctions.DocumentRegister.Remote.IsRegistrationNumberUnique(register, document,
                                                                                        recognizedNumber.Number, 0, recognizedDate.Date.Value,
                                                                                        depCode, bunitCode,
                                                                                        caseIndex, kindCode,
                                                                                        counterpartyCode, leadingDocumentId))
        {
          documentInfo.RegistrationFailed = true;
          return;
        }
      }
      
      // Не сохранять документ при нумерации, чтобы не потерять параметр DocumentNumberingBySmartCaptureResult.
      Docflow.PublicFunctions.OfficialDocument.RegisterDocument(document, register, recognizedDate.Date, recognizedNumber.Number, false, false);
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        recognizedDate.Fact,
                                                                        ArioGrammars.DocumentFact.DateField,
                                                                        props.RegistrationDate.Name,
                                                                        document.RegistrationDate,
                                                                        recognizedDate.Probability);
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        recognizedNumber.Fact,
                                                                        ArioGrammars.DocumentFact.NumberField,
                                                                        props.RegistrationNumber.Name,
                                                                        document.RegistrationNumber,
                                                                        recognizedNumber.Probability);
    }
    
    /// <summary>
    /// Заполнить дату и номер документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="factName">Наименование факта с датой и номером документа.</param>
    /// <param name="withoutNumberLabel">Замещающий текст для номера, если он не распознан или отсутствует. По умолчанию "б/н".</param>
    [Public]
    public virtual void FillDocumentNumberAndDate(IOfficialDocument document,
                                                  IDocumentInfo documentInfo,
                                                  string factName,
                                                  string withoutNumberLabel)
    {
      var arioDocument = documentInfo.ArioDocument;
      var props = document.Info.Properties;

      // Дата.
      var recognizedDate = this.GetRecognizedDate(arioDocument.Facts, factName,
                                                  ArioGrammars.DocumentFact.DateField);

      if (recognizedDate.Fact != null)
      {
        document.RegistrationDate = recognizedDate.Date;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedDate.Fact,
                                                                          ArioGrammars.DocumentFact.DateField,
                                                                          props.RegistrationDate.Name,
                                                                          recognizedDate.Date,
                                                                          recognizedDate.Probability);
      }

      // Номер.
      var regNumberPropertyInfo = props.RegistrationNumber;
      var recognizedNumber = this.GetRecognizedNumber(arioDocument.Facts, factName,
                                                      ArioGrammars.DocumentFact.NumberField,
                                                      regNumberPropertyInfo);
      
      // Если номер не распознан или пришло пустое значение, то вероятность минимальная.
      if (string.IsNullOrWhiteSpace(recognizedNumber.Number) || recognizedNumber.Number == string.Empty)
      {
        recognizedNumber.Number = string.Empty;
        recognizedNumber.Probability = Module.PropertyProbabilityLevels.Min;
      }
      
      // Список обозначений "без номера" аналогичен синхронизации с 1С.
      var emptyNumberSymbols = new List<string>
      {
        Resources.WithoutNumberWithSlash,
        Resources.WithoutNumber,
        Resources.WithoutNumberWithDash,
        string.Empty
      };

      // Если номер не распознан или документ пришел без номера, то подставить заданное значение.
      if (emptyNumberSymbols.Contains(recognizedNumber.Number.ToLower()))
        recognizedNumber.Number = string.IsNullOrEmpty(withoutNumberLabel) ? Docflow.Resources.DocumentWithoutNumber : withoutNumberLabel;

      document.RegistrationNumber = recognizedNumber.Number;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        recognizedNumber.Fact,
                                                                        ArioGrammars.DocumentFact.NumberField,
                                                                        props.RegistrationNumber.Name,
                                                                        document.RegistrationNumber,
                                                                        recognizedNumber.Probability);
    }
    
    #endregion
    
    #region Делопроизводство
    
    /// <summary>
    /// Заполнить свойства входящего письма по результатам обработки Ario.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillIncomingLetterProperties(RecordManagement.IIncomingLetter letter,
                                                     IDocumentInfo documentInfo,
                                                     Sungero.Company.IEmployee responsible)
    {
      var props = letter.Info.Properties;
      var arioDocument = documentInfo.ArioDocument;

      // Вид документа.
      this.FillDocumentKind(letter);
      
      // Содержание.
      this.FillIncomingLetterSubject(letter, arioDocument);
      
      // Дата.
      var recognizedDate = this.GetRecognizedDate(arioDocument.Facts, ArioGrammars.LetterFact.Name, ArioGrammars.LetterFact.DateField);
      if (recognizedDate.Fact != null)
      {
        letter.Dated = recognizedDate.Date;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedDate.Fact,
                                                                          ArioGrammars.LetterFact.DateField,
                                                                          props.Dated.Name,
                                                                          recognizedDate.Date,
                                                                          recognizedDate.Probability);
      }
      
      // Номер.
      var numberPropertyInfo = props.InNumber;
      var recognizedNumber = this.GetRecognizedNumber(arioDocument.Facts,
                                                      ArioGrammars.LetterFact.Name,
                                                      ArioGrammars.LetterFact.NumberField,
                                                      numberPropertyInfo);
      if (recognizedNumber.Fact != null)
      {
        letter.InNumber = recognizedNumber.Number;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedNumber.Fact,
                                                                          ArioGrammars.LetterFact.NumberField,
                                                                          props.InNumber.Name,
                                                                          letter.InNumber,
                                                                          recognizedNumber.Probability);
      }
      
      // Заполнить данные корреспондента.
      var correspondent = this.GetRecognizedCounterparty(arioDocument.Facts,
                                                         props.Correspondent.Name,
                                                         ArioGrammars.LetterFact.Name,
                                                         ArioGrammars.LetterFact.CorrespondentNameField,
                                                         ArioGrammars.LetterFact.CorrespondentLegalFormField);

      if (correspondent != null)
      {
        letter.Correspondent = correspondent.Counterparty;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          correspondent.Fact,
                                                                          null,
                                                                          props.Correspondent.Name,
                                                                          letter.Correspondent,
                                                                          correspondent.CounterpartyProbability);
      }
      
      // Заполнить данные нашей стороны.
      // Убираем уже использованный факт для подбора контрагента,
      // чтобы организация и адресат не искались по тем же реквизитам, что и контрагент.
      if (correspondent != null)
        arioDocument.Facts.Remove(correspondent.Fact);
      
      this.FillIncomingLetterToProperties(letter, documentInfo, responsible);
      
      var personFacts = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                       ArioGrammars.LetterPersonFact.Name,
                                                                       ArioGrammars.LetterPersonFact.SurnameField);
      // Заполнить подписанта.
      this.FillIncomingLetterSignedBy(letter, documentInfo, personFacts);
      
      // Заполнить контакт.
      this.FillIncomingLetterContact(letter, documentInfo, personFacts);
    }
    
    /// <summary>
    /// Заполнить контакт входящего письма.
    /// </summary>
    /// <param name="document">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="facts">Факты, содержащие информацию о персонах.</param>
    [Public]
    public virtual void FillIncomingLetterContact(IIncomingLetter document,
                                                  IDocumentInfo documentInfo,
                                                  List<IArioFact> facts)
    {
      var recognitionInfo = documentInfo.ArioDocument.RecognitionInfo;
      var props = document.Info.Properties;
      var responsibleFact = facts
        .Where(x => Commons.PublicFunctions.Module.GetFieldValue(x, ArioGrammars.LetterPersonFact.TypeField) == ArioGrammars.LetterPersonFact.PersonTypes.Responsible)
        .FirstOrDefault();

      var recognizedResponsibleNaming = this.GetRecognizedPersonNaming(responsibleFact,
                                                                       ArioGrammars.LetterPersonFact.SurnameField,
                                                                       ArioGrammars.LetterPersonFact.NameField,
                                                                       ArioGrammars.LetterPersonFact.PatrnField);
      
      var contact = this.GetRecognizedContact(responsibleFact, props.Contact.Name, document.Correspondent,
                                              props.Correspondent.Name, recognizedResponsibleNaming);
      
      // При заполнении полей подписал и контакт, если контрагент не заполнен, он подставляется из подписанта/контакта.
      if (document.Correspondent == null && contact.Contact != null)
      {
        // Если вероятность определения подписанта больше уровня "выше среднего", то установить вероятность определения КА "выше среднего",
        // иначе установить минимальную вероятность.
        var recognizedContactProbability = contact.Probability >= Module.PropertyProbabilityLevels.UpperMiddle ?
          Module.PropertyProbabilityLevels.UpperMiddle :
          Module.PropertyProbabilityLevels.Min;
        
        // Если запись контрагента - закрытая, то установить минимальную вероятность. bug 104160
        if (contact.Contact.Company.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          recognizedContactProbability = Module.PropertyProbabilityLevels.Min;
        
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, null,
                                                                          null, props.Correspondent.Name,
                                                                          contact.Contact.Company,
                                                                          recognizedContactProbability);
      }
      
      document.Contact = contact.Contact;
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, responsibleFact,
                                                                        null, props.Contact.Name,
                                                                        document.Contact,
                                                                        contact.Probability);
    }
    
    /// <summary>
    /// Заполнить подписанта входящего письма.
    /// </summary>
    /// <param name="document">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="facts">Факты, содержащие информацию о персонах.</param>
    [Public]
    public virtual void FillIncomingLetterSignedBy(IIncomingLetter document,
                                                   IDocumentInfo documentInfo,
                                                   List<IArioFact> facts)
    {
      var recognitionInfo = documentInfo.ArioDocument.RecognitionInfo;
      var props = document.Info.Properties;
      var signatoryFact = facts
        .Where(x => Commons.PublicFunctions.Module.GetFieldValue(x, ArioGrammars.LetterPersonFact.TypeField) == ArioGrammars.LetterPersonFact.PersonTypes.Signatory)
        .FirstOrDefault();
      
      var recognizedSignatoryNaming = this.GetRecognizedPersonNaming(signatoryFact,
                                                                     ArioGrammars.LetterPersonFact.SurnameField,
                                                                     ArioGrammars.LetterPersonFact.NameField,
                                                                     ArioGrammars.LetterPersonFact.PatrnField);
      
      var signedBy = this.GetRecognizedContact(signatoryFact, props.SignedBy.Name, document.Correspondent,
                                               props.Correspondent.Name, recognizedSignatoryNaming);
      
      // При заполнении полей подписал и контакт, если контрагент не заполнен, он подставляется из подписанта/контакта.
      if (document.Correspondent == null && signedBy.Contact != null)
      {
        // Если вероятность определения подписанта больше уровня "выше среднего", то установить вероятность определения КА "выше среднего",
        // иначе установить минимальную вероятность.
        var recognizedCorrespondentProbability = signedBy.Probability >= Module.PropertyProbabilityLevels.UpperMiddle ?
          Module.PropertyProbabilityLevels.UpperMiddle :
          Module.PropertyProbabilityLevels.Min;
        
        // Если запись контрагента - закрытая, то установить минимальную вероятность. bug 104160
        if (signedBy.Contact.Company.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          recognizedCorrespondentProbability = Module.PropertyProbabilityLevels.Min;
        
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, null, null,
                                                                          props.Correspondent.Name,
                                                                          signedBy.Contact.Company,
                                                                          recognizedCorrespondentProbability);
      }
      
      document.SignedBy = signedBy.Contact;

      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, signatoryFact,
                                                                        null, props.SignedBy.Name,
                                                                        document.SignedBy,
                                                                        signedBy.Probability);
    }
    
    /// <summary>
    /// Заполнить данные нашей стороны (НОР, подразделение, адресата).
    /// </summary>
    /// <param name="document">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillIncomingLetterToProperties(IIncomingLetter document,
                                                       IDocumentInfo documentInfo,
                                                       IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      var recognitionInfo = arioDocument.RecognitionInfo;
      var props = document.Info.Properties;
      
      // Заполнить адресата.
      this.FillIncomingLetterAddressee(document, documentInfo);
      
      // Заполнить НОР.
      var recognizedBusinessUnit = RecognizedCounterparty.Create();
      var recognizedBusinessUnits = this.GetRecognizedBusinessUnits(arioDocument.Facts,
                                                                    ArioGrammars.LetterFact.Name,
                                                                    ArioGrammars.LetterFact.CorrespondentNameField,
                                                                    ArioGrammars.LetterFact.CorrespondentLegalFormField);
      
      // Если для свойства businessUnitPropertyName по факту существует верифицированное ранее значение, то вернуть его.
      foreach (var fact in recognizedBusinessUnits.Select(x => x.Fact))
      {
        var previousRecognizedBusinessUnit = this.GetPreviousBusinessUnitRecognitionResults(fact, props.BusinessUnit.Name);
        if (previousRecognizedBusinessUnit != null && previousRecognizedBusinessUnit.BusinessUnit != null)
        {
          recognizedBusinessUnit = previousRecognizedBusinessUnit;
          break;
        }
      }
      
      if (recognizedBusinessUnit.BusinessUnit == null)
      {
        // Если по фактам нашлась одна НОР, то ее и подставляем в документ.
        if (recognizedBusinessUnits.Count == 1)
          recognizedBusinessUnit = recognizedBusinessUnits.FirstOrDefault();
        
        // Если по фактам нашлось несколько НОР, то берем наиболее вероятную.
        if (recognizedBusinessUnits.Count > 1)
          recognizedBusinessUnit = recognizedBusinessUnits.OrderByDescending(x => x.BusinessUnitProbability).FirstOrDefault();
      }
      
      // Если не удалось найти НОР по фактам, то попытаться определить НОР от адресата.
      if (recognizedBusinessUnit.BusinessUnit == null)
      {
        // Получить НОР адресата.
        var businessUnitByAddressee = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(document.Addressee);
        // Вероятность распознавания адресата.
        var addresseeProbability = recognitionInfo.Facts.Where(x => x.PropertyName == props.Addressee.Name)
          .Select(x => x.Probability)
          .FirstOrDefault();
        if (businessUnitByAddressee != null)
        {
          recognizedBusinessUnit.BusinessUnit = businessUnitByAddressee;
          recognizedBusinessUnit.Fact = null;
          // Если вероятность определения подписанта больше уровня "выше среднего", то установить вероятность определения НОР "выше среднего",
          // иначе установить минимальную вероятность.
          recognizedBusinessUnit.BusinessUnitProbability = addresseeProbability >= Module.PropertyProbabilityLevels.UpperMiddle ?
            Module.PropertyProbabilityLevels.UpperMiddle :
            Module.PropertyProbabilityLevels.Min;
        }
      }
      
      // Если и по адресату НОР не найдена, то вернуть НОР из персональных настроек или карточки ответственного.
      if (recognizedBusinessUnit.BusinessUnit == null)
      {
        recognizedBusinessUnit.BusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);
        recognizedBusinessUnit.Fact = null;
        recognizedBusinessUnit.BusinessUnitProbability = Module.PropertyProbabilityLevels.Min;
      }
      
      document.BusinessUnit = recognizedBusinessUnit.BusinessUnit;
      
      // Если запись НОР - закрытая, то установить минимальную вероятность. bug 104160
      if (recognizedBusinessUnit.BusinessUnit != null && recognizedBusinessUnit.BusinessUnit.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        recognizedBusinessUnit.BusinessUnitProbability = Module.PropertyProbabilityLevels.Min;
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, recognizedBusinessUnit.Fact,
                                                                        null, props.BusinessUnit.Name,
                                                                        document.BusinessUnit, recognizedBusinessUnit.BusinessUnitProbability);
      // Заполнить подразделение.
      document.Department = document.Addressee != null
        ? Company.PublicFunctions.Department.GetDepartment(document.Addressee)
        : Company.PublicFunctions.Department.GetDepartment(responsible);
    }
    
    /// <summary>
    /// Заполнить адресата входящего письма.
    /// </summary>
    /// <param name="document">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    public virtual void FillIncomingLetterAddressee(IIncomingLetter document,
                                                    IDocumentInfo documentInfo)
    {
      var addresseeFact = Commons.PublicFunctions.Module.GetOrderedFacts(documentInfo.ArioDocument.Facts,
                                                                         ArioGrammars.LetterFact.Name,
                                                                         ArioGrammars.LetterFact.AddresseeField)
        .FirstOrDefault();
      
      var addresseeName = Commons.PublicFunctions.Module.GetFieldValue(addresseeFact,
                                                                       ArioGrammars.LetterFact.AddresseeField);
      if (string.IsNullOrEmpty(addresseeName))
        return;
      
      var employees = this.GetAddresseesByName(addresseeName);
      if (!employees.Any())
        return;
      
      var fieldProbability = Commons.PublicFunctions.Module.GetFieldProbability(addresseeFact, ArioGrammars.LetterFact.AddresseeField);
      var probability = fieldProbability / employees.Count();
      
      document.Addressee = employees.FirstOrDefault();
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(documentInfo.ArioDocument.RecognitionInfo,
                                                                        addresseeFact,
                                                                        ArioGrammars.LetterFact.AddresseeField,
                                                                        document.Info.Properties.Addressee.Name,
                                                                        document.Addressee,
                                                                        probability);
    }

    /// <summary>
    /// Заполнить содержание входящего письма.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    [Public]
    public virtual void FillIncomingLetterSubject(IIncomingLetter letter, IArioDocument arioDocument)
    {
      var subjectFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                       ArioGrammars.LetterFact.Name,
                                                                       ArioGrammars.LetterFact.SubjectField)
        .FirstOrDefault();
      
      var subject = Commons.PublicFunctions.Module.GetFieldValue(subjectFact, ArioGrammars.LetterFact.SubjectField);
      if (!string.IsNullOrEmpty(subject))
      {
        letter.Subject = string.Format("{0}{1}", subject.Substring(0, 1).ToUpper(), subject.Remove(0, 1).ToLower());
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          subjectFact,
                                                                          ArioGrammars.LetterFact.SubjectField,
                                                                          letter.Info.Properties.Subject.Name,
                                                                          letter.Subject);
      }
    }
    #endregion
    
    #region Договорные документы и счет на оплату

    /// <summary>
    /// Заполнить свойства договора по результатам обработки Ario.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillContractProperties(Contracts.IContract contract,
                                               IDocumentInfo documentInfo,
                                               Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(contract);
      
      this.FillContractualDocumentAmountAndCurrency(contract, documentInfo);
      
      // Заполнить данные нашей стороны и корреспондента.
      this.FillContractualDocumentParties(contract, documentInfo, responsible);
      
      // Заполнить ответственного после заполнения НОР и КА, чтобы вычислялась НОР из фактов, а не по отв.
      // Если так не сделать, то НОР заполнится по ответственному и вычисления не будут выполняться.
      contract.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      contract.ResponsibleEmployee = responsible;
      
      // Дата и номер.
      this.FillDocumentRegistrationData(contract, documentInfo, ArioGrammars.DocumentFact.Name, Docflow.Resources.DocumentWithoutNumber);
    }
    
    /// <summary>
    /// Заполнить свойства доп. соглашения по результатам обработки Ario.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillSupAgreementProperties(Contracts.ISupAgreement supAgreement,
                                                   IDocumentInfo documentInfo,
                                                   Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(supAgreement);

      this.FillContractualDocumentAmountAndCurrency(supAgreement, documentInfo);
      
      // Заполнить данные нашей стороны и корреспондента.
      this.FillContractualDocumentParties(supAgreement, documentInfo, responsible);
      
      // Заполнить ответственного после заполнения НОР и КА, чтобы вычислялась НОР из фактов, а не по отв.
      // Если так не сделать, то НОР заполнится по ответственному и вычисления не будут выполняться.
      supAgreement.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      supAgreement.ResponsibleEmployee = responsible;
      
      // Дата и номер.
      this.FillDocumentRegistrationData(supAgreement, documentInfo, ArioGrammars.SupAgreementFact.Name, Docflow.Resources.DocumentWithoutNumber);
    }
    
    /// <summary>
    /// Заполнить сумму и валюту в договорных документах.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillContractualDocumentAmountAndCurrency(Docflow.IContractualDocumentBase contractualDocument,
                                                                 IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var recognizedAmount = this.GetRecognizedAmount(arioDocument.Facts);
      if (recognizedAmount.HasValue)
      {
        contractualDocument.TotalAmount = recognizedAmount.Amount;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedAmount.Fact,
                                                                          ArioGrammars.DocumentAmountFact.AmountField,
                                                                          contractualDocument.Info.Properties.TotalAmount.Name,
                                                                          recognizedAmount.Amount,
                                                                          recognizedAmount.Probability);
      }

      // В факте с суммой документа может быть не указана валюта, поэтому факт с валютой ищем отдельно,
      // так как на данный момент функция используется для обработки бухгалтерских и договорных документов,
      // а в них все расчеты ведутся в одной валюте.
      var recognizedCurrency = this.GetRecognizedCurrency(arioDocument.Facts);
      
      if (recognizedAmount.HasValue && !recognizedCurrency.HasValue)
      {
        recognizedCurrency.HasValue = true;
        recognizedCurrency.Currency = Commons.Currencies.GetAllCached(c => c.IsDefault == true).FirstOrDefault();
        recognizedCurrency.Probability = Module.PropertyProbabilityLevels.Min;
      }
      
      if (recognizedCurrency.HasValue)
      {
        contractualDocument.Currency = recognizedCurrency.Currency;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedCurrency.Fact,
                                                                          ArioGrammars.DocumentAmountFact.CurrencyField,
                                                                          contractualDocument.Info.Properties.Currency.Name,
                                                                          recognizedCurrency.Currency,
                                                                          recognizedCurrency.Probability);
      }
    }

    /// <summary>
    /// Заполнить стороны договорного документа.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillContractualDocumentParties(Contracts.IContractualDocument contractualDocument,
                                                       IDocumentInfo documentInfo,
                                                       IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      var props = contractualDocument.Info.Properties;
      var businessUnitPropertyName = props.BusinessUnit.Name;
      var counterpartyPropertyName = props.Counterparty.Name;
      
      var signatoryFieldNames = new List<string>
      {
        ArioGrammars.CounterpartyFact.SignatorySurnameField,
        ArioGrammars.CounterpartyFact.SignatoryNameField,
        ArioGrammars.CounterpartyFact.SignatoryPatrnField
      };
      
      var counterpartyFieldNames = new List<string>
      {
        ArioGrammars.CounterpartyFact.NameField,
        ArioGrammars.CounterpartyFact.LegalFormField,
        ArioGrammars.CounterpartyFact.CounterpartyTypeField,
        ArioGrammars.CounterpartyFact.TinField,
        ArioGrammars.CounterpartyFact.TinIsValidField,
        ArioGrammars.CounterpartyFact.TrrcField
      };
      
      // Заполнить данные нашей стороны.
      // Наша организация по фактам из Арио.
      var recognizedBusinessUnit = this.GetRecognizedBusinessUnitForContractualDocument(arioDocument.Facts, responsible);
      if (recognizedBusinessUnit.BusinessUnit != null)
      {
        contractualDocument.BusinessUnit = recognizedBusinessUnit.BusinessUnit;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                recognizedBusinessUnit.Fact,
                                                                                counterpartyFieldNames,
                                                                                businessUnitPropertyName,
                                                                                contractualDocument.BusinessUnit,
                                                                                recognizedBusinessUnit.BusinessUnitProbability,
                                                                                null);
      }
      
      // Заполнить подписанта.
      var ourSignatory = this.GetSignatoryForContractualDocument(contractualDocument,
                                                                 arioDocument.Facts,
                                                                 recognizedBusinessUnit,
                                                                 signatoryFieldNames,
                                                                 true);
      
      // При заполнении поля подписал, если НОР не заполнена, она подставляется из подписанта.
      if (ourSignatory.Employee != null)
        this.FillOurSignatoryForContractualDocument(contractualDocument, documentInfo, ourSignatory, signatoryFieldNames);
      
      // Если НОР по фактам не нашли, то взять ее из персональных настроек, или от ответственного.
      if (contractualDocument.BusinessUnit == null)
      {
        var responsibleEmployeeBusinessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(responsible);
        var responsibleEmployeePersonalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(responsible);
        var responsibleEmployeePersonalSettingsBusinessUnit = responsibleEmployeePersonalSettings != null
          ? responsibleEmployeePersonalSettings.BusinessUnit
          : Company.BusinessUnits.Null;

        // Если в персональных настройках ответственного указана НОР.
        if (responsibleEmployeePersonalSettingsBusinessUnit != null)
          contractualDocument.BusinessUnit = responsibleEmployeePersonalSettingsBusinessUnit;
        else
          contractualDocument.BusinessUnit = responsibleEmployeeBusinessUnit;
        
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(documentInfo.ArioDocument.RecognitionInfo,
                                                                                null,
                                                                                null,
                                                                                contractualDocument.Info.Properties.BusinessUnit.Name,
                                                                                contractualDocument.BusinessUnit,
                                                                                Module.PropertyProbabilityLevels.Min,
                                                                                null);
      }

      // Убрать использованные факты подбора НОР и подписывающего с нашей стороны.
      if (recognizedBusinessUnit.Fact != null)
        arioDocument.Facts.Remove(recognizedBusinessUnit.Fact);
      if (ourSignatory.Fact != null &&
          (recognizedBusinessUnit.Fact == null || ourSignatory.Fact.Id != recognizedBusinessUnit.Fact.Id))
        arioDocument.Facts.Remove(ourSignatory.Fact);

      // Заполнить данные контрагента.
      var counterparty = this.GetRecognizedCounterparty(arioDocument.Facts,
                                                        counterpartyPropertyName,
                                                        ArioGrammars.CounterpartyFact.Name,
                                                        ArioGrammars.CounterpartyFact.NameField,
                                                        ArioGrammars.CounterpartyFact.LegalFormField);
      
      if (counterparty != null)
      {
        contractualDocument.Counterparty = counterparty.Counterparty;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                counterparty.Fact,
                                                                                counterpartyFieldNames,
                                                                                counterpartyPropertyName,
                                                                                contractualDocument.Counterparty,
                                                                                counterparty.CounterpartyProbability,
                                                                                null);
      }
      
      // Заполнить подписанта от КА.
      var signedBy = this.GetSignatoryForContractualDocument(contractualDocument,
                                                             arioDocument.Facts,
                                                             counterparty,
                                                             signatoryFieldNames,
                                                             false);
      
      // При заполнении поля подписал, если контрагент не заполнен, он подставляется из подписанта.
      if (signedBy.Contact != null)
        this.FillCounterpartySignatoryForContractualDocument(contractualDocument, documentInfo, signedBy, signatoryFieldNames);
      
    }

    /// <summary>
    /// Заполнить подписанта НОР.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="ourSignatory">Информация о подписанте.</param>
    /// <param name="signatoryFieldNames">Список полей факта о подписанте.</param>
    public virtual void FillOurSignatoryForContractualDocument(Contracts.IContractualDocument contractualDocument,
                                                               IDocumentInfo documentInfo,
                                                               IRecognizedOfficial ourSignatory,
                                                               List<string> signatoryFieldNames)
    {
      if (contractualDocument.BusinessUnit == null &&
          ourSignatory.Employee.Department != null &&
          ourSignatory.Employee.Department.BusinessUnit != null)
      {
        // Если вероятность определения подписанта больше уровня "выше среднего", то установить вероятность определения НОР "выше среднего",
        // иначе установить минимальную вероятность.
        var recognizedBusinessUnitProbability = ourSignatory.Probability >= Module.PropertyProbabilityLevels.UpperMiddle ?
          Module.PropertyProbabilityLevels.UpperMiddle :
          Module.PropertyProbabilityLevels.Min;
        
        // Если запись НОР - закрытая, то установить минимальную вероятность. bug 104069
        if (ourSignatory.Employee.Department.BusinessUnit.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          recognizedBusinessUnitProbability = Module.PropertyProbabilityLevels.Min;
        contractualDocument.BusinessUnit = ourSignatory.Employee.Department.BusinessUnit;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(documentInfo.ArioDocument.RecognitionInfo,
                                                                          null,
                                                                          null,
                                                                          contractualDocument.Info.Properties.BusinessUnit.Name,
                                                                          ourSignatory.Employee.Department.BusinessUnit,
                                                                          recognizedBusinessUnitProbability);
      }
      
      contractualDocument.OurSignatory = ourSignatory.Employee;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(documentInfo.ArioDocument.RecognitionInfo,
                                                                              ourSignatory.Fact,
                                                                              signatoryFieldNames,
                                                                              contractualDocument.Info.Properties.OurSignatory.Name,
                                                                              contractualDocument.OurSignatory,
                                                                              ourSignatory.Probability,
                                                                              null);
    }
    
    /// <summary>
    /// Заполнить подписанта контрагента.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="signedBy">Информация о подписанте.</param>
    /// <param name="signatoryFieldNames">Список полей факта о подписанте.</param>
    public virtual void FillCounterpartySignatoryForContractualDocument(Contracts.IContractualDocument contractualDocument,
                                                                        IDocumentInfo documentInfo,
                                                                        IRecognizedOfficial signedBy,
                                                                        List<string> signatoryFieldNames)
    {
      var arioDocument = documentInfo.ArioDocument;
      var props = contractualDocument.Info.Properties;
      // Если контрагент не заполнен, взять его из подписанта.
      if (contractualDocument.Counterparty == null && signedBy.Contact.Company != null)
      {
        // Если вероятность определения подписанта больше уровня "выше среднего", то установить вероятность определения КА "выше среднего",
        // иначе установить минимальную вероятность.
        var recognizedCounterpartyProbability = signedBy.Probability >= Module.PropertyProbabilityLevels.UpperMiddle ?
          Module.PropertyProbabilityLevels.UpperMiddle :
          Module.PropertyProbabilityLevels.Min;
        
        // Если запись контрагента - закрытая, то установить минимальную вероятность. bug 104069
        if (signedBy.Contact.Company.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          recognizedCounterpartyProbability = Module.PropertyProbabilityLevels.Min;
        
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                null,
                                                                                null,
                                                                                props.Counterparty.Name,
                                                                                signedBy.Contact.Company,
                                                                                recognizedCounterpartyProbability,
                                                                                null);
      }
      
      contractualDocument.CounterpartySignatory = signedBy.Contact;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                              signedBy.Fact,
                                                                              signatoryFieldNames,
                                                                              props.CounterpartySignatory.Name,
                                                                              contractualDocument.CounterpartySignatory,
                                                                              signedBy.Probability,
                                                                              null);
    }

    /// <summary>
    /// Получить подписанта нашей стороны/подписанта контрагента для договорного документа по фактам и НОР.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="recognizedOrganization">Структура с НОР, КА, фактом и признаком доверия.</param>
    /// <param name="signatoryFieldNames">Список наименований полей с ФИО подписанта.</param>
    /// <param name="isOurSignatory">Признак поиска нашего подписанта (true) или подписанта КА (false).</param>
    /// <returns>Структура, содержащая сотрудника или контакт, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetSignatoryForContractualDocument(Contracts.IContractualDocument document,
                                                                          List<IArioFact> facts,
                                                                          IRecognizedCounterparty recognizedOrganization,
                                                                          List<string> signatoryFieldNames,
                                                                          bool isOurSignatory = false)
    {
      var props = document.Info.Properties;
      var signatoryFacts = Commons.PublicFunctions.Module.GetFacts(facts, ArioGrammars.CounterpartyFact.Name);
      var signedBy = RecognizedOfficial.Create(null, null, null, Module.PropertyProbabilityLevels.Min);
      
      if (!signatoryFacts.Any())
        return signedBy;
      
      if (recognizedOrganization != null)
      {
        var organizationFact = recognizedOrganization.Fact;
        if (organizationFact != null)
        {
          signedBy.Fact = organizationFact;
          var isOrganizationFactWithSignatory = Commons.PublicFunctions.Module.GetFields(organizationFact, signatoryFieldNames).Any();
          
          var recognizedOrganizationNaming = this.GetRecognizedPersonNaming(organizationFact,
                                                                            ArioGrammars.CounterpartyFact.SignatorySurnameField,
                                                                            ArioGrammars.CounterpartyFact.SignatoryNameField,
                                                                            ArioGrammars.CounterpartyFact.SignatoryPatrnField);
          if (isOrganizationFactWithSignatory)
            return isOurSignatory
              ? this.GetRecognizedOurSignatoryForContractualDocument(document, organizationFact, recognizedOrganizationNaming)
              : this.GetRecognizedContact(organizationFact, props.CounterpartySignatory.Name, document.Counterparty,
                                          props.Counterparty.Name, recognizedOrganizationNaming);
        }
        
        if (recognizedOrganization.BusinessUnit != null || recognizedOrganization.Counterparty != null)
          signatoryFacts = this.GetSpecifiedSignatoryByNameAndLegalForm(signatoryFacts, recognizedOrganization, isOurSignatory);
      }
      
      signatoryFacts = signatoryFacts
        .Where(f => Commons.PublicFunctions.Module.GetFields(f, signatoryFieldNames).Any()).ToList();
      
      var organizationSignatory = new List<IRecognizedOfficial>();
      foreach (var signatoryFact in signatoryFacts)
      {
        IRecognizedOfficial signatory = null;
        
        var recognizedSignatoryNaming = this.GetRecognizedPersonNaming(signatoryFact,
                                                                       ArioGrammars.CounterpartyFact.SignatorySurnameField,
                                                                       ArioGrammars.CounterpartyFact.SignatoryNameField,
                                                                       ArioGrammars.CounterpartyFact.SignatoryPatrnField);
        if (isOurSignatory)
        {
          signatory = this.GetRecognizedOurSignatoryForContractualDocument(document, signatoryFact, recognizedSignatoryNaming);
          if (signatory.Employee != null)
            organizationSignatory.Add(signatory);
        }
        else
        {
          signatory = this.GetRecognizedContact(signatoryFact, props.CounterpartySignatory.Name, document.Counterparty,
                                                props.Counterparty.Name, recognizedSignatoryNaming);
          if (signatory.Contact != null)
            organizationSignatory.Add(signatory);
        }
      }
      
      if (!organizationSignatory.Any())
        return signedBy;
      
      return organizationSignatory.OrderByDescending(x => x.Probability).FirstOrDefault();
    }

    /// <summary>
    /// Получить список фактов о подписанте, уточненный по наименованию организации и ОПФ.
    /// </summary>
    /// <param name="signatoryFacts">Список фактов.</param>
    /// <param name="recognizedOrganization">Структура с НОР, КА, фактом и признаком доверия.</param>
    /// <param name="isOurSignatory">Признак поиска нашего подписанта (true) или подписанта КА (false).</param>
    /// <returns>Уточненный список фактов.</returns>
    public virtual List<IArioFact> GetSpecifiedSignatoryByNameAndLegalForm(List<IArioFact> signatoryFacts, IRecognizedCounterparty recognizedOrganization, bool isOurSignatory)
    {
      var organizationName = isOurSignatory ?
        recognizedOrganization.BusinessUnit.Name :
        recognizedOrganization.Counterparty.Name;
      
      // Ожидаемое наименование НОР или организации в формате {Название}, {ОПФ}.
      var organizationNameAndLegalForm = organizationName.Split(new string[] { ", " }, StringSplitOptions.None);
      
      // Уточнить по наименованию.
      var specifiedSignatoryFacts = signatoryFacts
        .Where(f => f.Fields.Any(fl => fl.Name == ArioGrammars.CounterpartyFact.NameField &&
                                 fl.Value.Equals(organizationNameAndLegalForm.FirstOrDefault(), StringComparison.InvariantCultureIgnoreCase)))
        .ToList();
      // Уточнить по ОПФ.
      if (organizationNameAndLegalForm.Length > 1)
      {
        specifiedSignatoryFacts = specifiedSignatoryFacts
          .Where(f => f.Fields.Any(fl => fl.Name == ArioGrammars.CounterpartyFact.LegalFormField &&
                                   fl.Value.Equals(organizationNameAndLegalForm.LastOrDefault(), StringComparison.InvariantCultureIgnoreCase)))
          .ToList();
      }
      return specifiedSignatoryFacts;
    }

    /// <summary>
    /// Получить подписанта нашей стороны для договорного документа по факту.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="ourSignatoryFact">Факт, содержащий сведения о подписанте нашей стороны.</param>
    /// <param name="recognizedOurSignatoryNaming">Полное и краткое ФИО подписанта нашей стороны.</param>
    /// <returns>Структура, содержащая сотрудника, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetRecognizedOurSignatoryForContractualDocument(Contracts.IContractualDocument document,
                                                                                       IArioFact ourSignatoryFact,
                                                                                       IRecognizedPersonNaming recognizedOurSignatoryNaming)
    {
      var signedBy = RecognizedOfficial.Create(null, null, ourSignatoryFact, Module.PropertyProbabilityLevels.Min);
      var businessUnit = document.BusinessUnit;

      if (ourSignatoryFact == null)
        return signedBy;

      // Если для свойства Подписал по факту существует верифицированное ранее значение, то вернуть его.
      signedBy = this.GetPreviousOurSignatoryRecognitionResults(ourSignatoryFact,
                                                                document.Info.Properties.OurSignatory.Name,
                                                                businessUnit,
                                                                document.Info.Properties.BusinessUnit.Name);
      if (signedBy.Employee != null)
        return signedBy;
      
      var fullName = recognizedOurSignatoryNaming.FullName;
      var shortName = recognizedOurSignatoryNaming.ShortName;
      var filteredEmployees = Company.PublicFunctions.Employee.Remote.GetEmployeesByName(fullName);
      
      if (businessUnit != null)
        filteredEmployees = filteredEmployees.Where(e => e.Department.BusinessUnit.Equals(businessUnit)).ToList();
      
      if (!filteredEmployees.Any())
        return signedBy;
      
      signedBy.Employee = filteredEmployees.FirstOrDefault();
      
      // Если сотрудник подобран по полному имени персоны и полное имя не эквивалентно короткому,
      // то считаем, что сотрудник определен с максимальной вероятностью,
      // иначе с вероятностью ниже среднего.
      signedBy.Probability = string.Equals(signedBy.Employee.Name, fullName, StringComparison.InvariantCultureIgnoreCase) &&
        !string.Equals(fullName, shortName, StringComparison.InvariantCultureIgnoreCase) ?
        Module.PropertyProbabilityLevels.Max / filteredEmployees.Count() :
        Module.PropertyProbabilityLevels.LowerMiddle / filteredEmployees.Count();
      
      return signedBy;
    }
    
    /// <summary>
    /// Поиск НОР для договорных документов по фактам Арио.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    /// <returns>НОР и соответствующий ей факт.</returns>
    [Public]
    public virtual IRecognizedCounterparty GetRecognizedBusinessUnitForContractualDocument(List<IArioFact> facts,
                                                                                           IEmployee responsible)
    {
      var businessUnitPropertyName = Sungero.Contracts.ContractualDocuments.Info.Properties.BusinessUnit.Name;
      var recognizedBusinessUnits = this.GetRecognizedBusinessUnits(facts,
                                                                    ArioGrammars.CounterpartyFact.Name,
                                                                    ArioGrammars.CounterpartyFact.NameField,
                                                                    ArioGrammars.CounterpartyFact.LegalFormField);
      
      var businessUnit = RecognizedCounterparty.Create();
      
      // Если для свойства businessUnitPropertyName по факту существует верифицированное ранее значение, то вернуть его.
      foreach (var fact in recognizedBusinessUnits.Select(x => x.Fact))
      {
        businessUnit = this.GetPreviousBusinessUnitRecognitionResults(fact, businessUnitPropertyName);
        if (businessUnit != null && businessUnit.BusinessUnit != null)
          return businessUnit;
      }
      
      // Если найдено несколько НОР, попытаться уточнить по ответственному за верификацию
      if (recognizedBusinessUnits.Count > 1)
      {
        var responsibleEmployeeBusinessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(responsible);
        var responsibleEmployeePersonalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(responsible);
        var responsibleEmployeePersonalSettingsBusinessUnit = responsibleEmployeePersonalSettings != null
          ? responsibleEmployeePersonalSettings.BusinessUnit
          : Company.BusinessUnits.Null;
        
        // Если в персональных настройках ответственного указана НОР, то отфильтровать по ней НОР, подобранные по фактам.
        if (responsibleEmployeePersonalSettingsBusinessUnit != null &&
            recognizedBusinessUnits.Any(x => Equals(x.BusinessUnit, responsibleEmployeePersonalSettingsBusinessUnit)))
          recognizedBusinessUnits = recognizedBusinessUnits.Where(x => Equals(x.BusinessUnit, responsibleEmployeePersonalSettingsBusinessUnit)).ToList();
        
        // Из НОР, подобранных по фактам, отфильтровать несоответствующие НОР ответственного.
        if (responsibleEmployeeBusinessUnit != null &&
            recognizedBusinessUnits.Any(x => Equals(x.BusinessUnit, responsibleEmployeeBusinessUnit)))
          recognizedBusinessUnits = recognizedBusinessUnits.Where(x => Equals(x.BusinessUnit, responsibleEmployeeBusinessUnit)).ToList();
      }
      
      // Если по фактам НОР не найдена.
      if (recognizedBusinessUnits.Count() == 0)
        return businessUnit;
      
      // Вернуть НОР с наибольшей вероятностью.
      return recognizedBusinessUnits.OrderByDescending(x => x.BusinessUnitProbability)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Заполнить свойства во входящем счете на оплату.
    /// </summary>
    /// <param name="incomingInvoice">Входящий счет.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillIncomingInvoiceProperties(Contracts.IIncomingInvoice incomingInvoice,
                                                      IDocumentInfo documentInfo,
                                                      IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(incomingInvoice);
      
      // Подразделение.
      incomingInvoice.Department = Company.PublicFunctions.Department.GetDepartment(responsible);

      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(incomingInvoice, documentInfo);
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
      arioCounterpartyTypes.Add(string.Empty);
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterparties(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault();
      var nonType = recognizedCounterparties.Where(m => m.Type == string.Empty).ToList();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, nonType, responsible);
      
      this.FillAccountingDocumentParties(incomingInvoice, documentInfo, recognizedDocumentParties);
      
      // Договор.
      var contractFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                        ArioGrammars.FinancialDocumentFact.Name,
                                                                        ArioGrammars.FinancialDocumentFact.DocumentBaseNameField)
        .FirstOrDefault();
      
      var contract = this.GetLeadingContractualDocument(contractFact,
                                                        incomingInvoice.Info.Properties.Contract.Name,
                                                        incomingInvoice.Counterparty,
                                                        incomingInvoice.Info.Properties.Counterparty.Name);
      incomingInvoice.Contract = contract.Contract;
      
      var props = incomingInvoice.Info.Properties;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        contractFact,
                                                                        null,
                                                                        props.Contract.Name,
                                                                        incomingInvoice.Contract,
                                                                        contract.Probability);
      
      // Дата.
      var recognizedDate = this.GetRecognizedDate(arioDocument.Facts,
                                                  ArioGrammars.FinancialDocumentFact.Name,
                                                  ArioGrammars.FinancialDocumentFact.DateField);
      if (recognizedDate.Fact != null)
      {
        incomingInvoice.Date = recognizedDate.Date;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedDate.Fact,
                                                                          ArioGrammars.FinancialDocumentFact.DateField,
                                                                          props.Date.Name,
                                                                          recognizedDate.Date,
                                                                          recognizedDate.Probability);
      }
      
      // Номер.
      var numberPropertyInfo = props.Number;
      var recognizedNumber = this.GetRecognizedNumber(arioDocument.Facts,
                                                      ArioGrammars.FinancialDocumentFact.Name,
                                                      ArioGrammars.FinancialDocumentFact.NumberField,
                                                      numberPropertyInfo);
      if (recognizedNumber.Fact != null)
      {
        incomingInvoice.Number = recognizedNumber.Number;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedNumber.Fact,
                                                                          ArioGrammars.FinancialDocumentFact.NumberField,
                                                                          props.Number.Name,
                                                                          incomingInvoice.Number,
                                                                          recognizedNumber.Probability);
      }
    }
    
    #endregion
    
    #region Первичка

    /// <summary>
    /// Заполнить свойства в акте.
    /// </summary>
    /// <param name="contractStatement">Акт.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillContractStatementProperties(FinancialArchive.IContractStatement contractStatement,
                                                        IDocumentInfo documentInfo,
                                                        IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(contractStatement);
      
      // Подразделение и ответственный.
      contractStatement.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      contractStatement.ResponsibleEmployee = responsible;
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(contractStatement, documentInfo);
      
      var props = contractStatement.Info.Properties;

      // НОР и КА.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
      arioCounterpartyTypes.Add(string.Empty);
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterparties(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault();
      var nonType = recognizedCounterparties.Where(m => m.Type == string.Empty).ToList();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, nonType, responsible);
      
      this.FillAccountingDocumentParties(contractStatement, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(contractStatement, documentInfo, ArioGrammars.DocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Договор.
      var leadingDocFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                          ArioGrammars.FinancialDocumentFact.Name,
                                                                          ArioGrammars.FinancialDocumentFact.DocumentBaseNameField)
        .FirstOrDefault();
      
      var leadingDocument = this.GetLeadingContractualDocument(leadingDocFact,
                                                               props.LeadingDocument.Name,
                                                               contractStatement.Counterparty, props.Counterparty.Name);
      contractStatement.LeadingDocument = leadingDocument.Contract;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        leadingDocFact,
                                                                        null,
                                                                        props.LeadingDocument.Name,
                                                                        contractStatement.LeadingDocument,
                                                                        leadingDocument.Probability);
    }
    
    /// <summary>
    /// Заполнить свойства выставленного счёта-фактуры по результатам обработки Ario.
    /// </summary>
    /// <param name="outgoingTaxInvoice">Выставленный счёт-фактура.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    /// <param name="recognizedDocumentParties">Результат подбора сторон сделки для документа.</param>
    [Public]
    public virtual void FillOutgoingTaxInvoiceProperties(FinancialArchive.IOutgoingTaxInvoice outgoingTaxInvoice,
                                                         IDocumentInfo documentInfo,
                                                         Sungero.Company.IEmployee responsible,
                                                         IRecognizedDocumentParties recognizedDocumentParties)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(outgoingTaxInvoice);
      
      // Подразделение.
      outgoingTaxInvoice.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(outgoingTaxInvoice, documentInfo);
      
      // НОР и КА.
      this.FillAccountingDocumentParties(outgoingTaxInvoice, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(outgoingTaxInvoice, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Корректировочный документ.
      if (outgoingTaxInvoice.IsAdjustment.HasValue && outgoingTaxInvoice.IsAdjustment.Value == true)
        this.FillOutgoingTaxInvoiceCorrectedDocument(outgoingTaxInvoice, documentInfo);
      else
        this.FillOutgoingTaxInvoiceRevisionInfo(outgoingTaxInvoice, documentInfo);
    }
    
    /// <summary>
    /// Заполнить свойства полученного счёта-фактуры по результатам обработки Ario.
    /// </summary>
    /// <param name="incomingTaxInvoice">Полученный счёт-фактура.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    /// <param name="recognizedDocumentParties">Результат подбора сторон сделки для документа.</param>
    [Public]
    public virtual void FillIncomingTaxInvoiceProperties(FinancialArchive.IIncomingTaxInvoice incomingTaxInvoice,
                                                         IDocumentInfo documentInfo,
                                                         Sungero.Company.IEmployee responsible,
                                                         IRecognizedDocumentParties recognizedDocumentParties)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(incomingTaxInvoice);
      
      // Подразделение.
      incomingTaxInvoice.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(incomingTaxInvoice, documentInfo);
      
      // НОР и КА.
      this.FillAccountingDocumentParties(incomingTaxInvoice, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(incomingTaxInvoice, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Корректировочный документ.
      if (incomingTaxInvoice.IsAdjustment.HasValue && incomingTaxInvoice.IsAdjustment.Value == true)
        this.FillIncomingTaxInvoiceCorrectedDocument(incomingTaxInvoice, documentInfo);
      else
        this.FillIncomingTaxInvoiceRevisionInfo(incomingTaxInvoice, documentInfo);
    }
    
    /// <summary>
    /// Заполнить свойства накладной по результатам обработки Ario.
    /// </summary>
    /// <param name="waybill">Товарная накладная.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillWaybillProperties(FinancialArchive.IWaybill waybill,
                                              IDocumentInfo documentInfo,
                                              Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(waybill);
      
      // Подразделение и ответственный.
      waybill.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      waybill.ResponsibleEmployee = responsible;
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(waybill, documentInfo);
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Supplier);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Payer);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterparties(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Supplier).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Payer).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee).FirstOrDefault();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, responsible);
      
      this.FillAccountingDocumentParties(waybill, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(waybill, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Документ-основание.
      var leadingDocFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                          ArioGrammars.FinancialDocumentFact.Name,
                                                                          ArioGrammars.FinancialDocumentFact.DocumentBaseNameField)
        .FirstOrDefault();
      
      var leadingDocument = this.GetLeadingContractualDocument(leadingDocFact,
                                                               waybill.Info.Properties.LeadingDocument.Name,
                                                               waybill.Counterparty, waybill.Info.Properties.Counterparty.Name);
      waybill.LeadingDocument = leadingDocument.Contract;
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo, leadingDocFact, null, waybill.Info.Properties.LeadingDocument.Name, waybill.LeadingDocument, leadingDocument.Probability);
    }
    
    /// <summary>
    /// Заполнить свойства универсального передаточного документа по результатам обработки Ario.
    /// </summary>
    /// <param name="universalTransferDocument">Универсальный передаточный документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillUniversalTransferDocumentProperties(FinancialArchive.IUniversalTransferDocument universalTransferDocument,
                                                                IDocumentInfo documentInfo,
                                                                Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(universalTransferDocument);
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(universalTransferDocument, documentInfo);
      
      // Подразделение и ответственный.
      universalTransferDocument.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      universalTransferDocument.ResponsibleEmployee = responsible;
      
      var props = universalTransferDocument.Info.Properties;
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);

      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterparties(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee).FirstOrDefault();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, responsible);
      
      this.FillAccountingDocumentParties(universalTransferDocument, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(universalTransferDocument, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Корректировочный документ.
      if (universalTransferDocument.IsAdjustment.HasValue && universalTransferDocument.IsAdjustment.Value == true)
        this.FillUniversalTransferDocumentCorrectedDocument(universalTransferDocument, documentInfo);
      else
        this.FillUniversalTransferDocumentRevisionInfo(universalTransferDocument, documentInfo);
    }

    /// <summary>
    /// Заполнить сумму и валюту в финансовом документе.
    /// </summary>
    /// <param name="accountingDocument">Финансовый документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillAccountingDocumentAmountAndCurrency(Docflow.IAccountingDocumentBase accountingDocument,
                                                                IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var recognizedAmount = this.GetRecognizedAmount(arioDocument.Facts);
      if (recognizedAmount.HasValue)
      {
        accountingDocument.TotalAmount = recognizedAmount.Amount;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedAmount.Fact,
                                                                          ArioGrammars.DocumentAmountFact.AmountField,
                                                                          accountingDocument.Info.Properties.TotalAmount.Name,
                                                                          recognizedAmount.Amount,
                                                                          recognizedAmount.Probability);
      }

      // В факте с суммой документа может быть не указана валюта, поэтому факт с валютой ищем отдельно,
      // так как на данный момент функция используется для обработки бухгалтерских и договорных документов,
      // а в них все расчеты ведутся в одной валюте.
      var recognizedCurrency = this.GetRecognizedCurrency(arioDocument.Facts);
      
      if (recognizedAmount.HasValue && !recognizedCurrency.HasValue)
      {
        recognizedCurrency.HasValue = true;
        recognizedCurrency.Currency = Commons.Currencies.GetAllCached(c => c.IsDefault == true).FirstOrDefault();
        recognizedCurrency.Probability = Module.PropertyProbabilityLevels.Min;
      }
      
      if (recognizedCurrency.HasValue)
      {
        accountingDocument.Currency = recognizedCurrency.Currency;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedCurrency.Fact,
                                                                          ArioGrammars.DocumentAmountFact.CurrencyField,
                                                                          accountingDocument.Info.Properties.Currency.Name,
                                                                          recognizedCurrency.Currency,
                                                                          recognizedCurrency.Probability);
      }
    }

    /// <summary>
    /// Заполнить корректируемый документ в полученном СФ.
    /// </summary>
    /// <param name="incomingTaxInvoice">Полученный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillIncomingTaxInvoiceCorrectedDocument(FinancialArchive.IIncomingTaxInvoice incomingTaxInvoice,
                                                                IDocumentInfo documentInfo)
    {
      this.FillIncomingTaxInvoicelCorrectedDocumentRevisionInfo(incomingTaxInvoice, documentInfo);
      
      var arioDocument = documentInfo.ArioDocument;
      var props = incomingTaxInvoice.Info.Properties;
      
      var correctionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionDateField)
        .FirstOrDefault();
      
      var correctionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(correctionDateFact, ArioGrammars.FinancialDocumentFact.CorrectionDateField);
      
      var correctionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                                ArioGrammars.FinancialDocumentFact.Name,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionNumberField)
        .FirstOrDefault();
      
      var correctionNumber = Commons.PublicFunctions.Module.GetFieldValue(correctionNumberFact, ArioGrammars.FinancialDocumentFact.CorrectionNumberField);
      
      var documents = FinancialArchive.IncomingTaxInvoices.GetAll()
        .Where(d => d.Id != incomingTaxInvoice.Id && d.IsAdjustment != true)
        .Where(d => d.RegistrationNumber.Equals(correctionNumber, StringComparison.InvariantCultureIgnoreCase) &&
               d.RegistrationDate == correctionDate &&
               incomingTaxInvoice.Counterparty != null &&
               Equals(d.Counterparty, incomingTaxInvoice.Counterparty));
      
      if (!documents.Any())
      {
        var correctionDateString = correctionDate == null ? string.Empty : correctionDate.Value.Date.ToString("d");
        var correctionString = Resources.TaxInvoiceCorrectsFormat(correctionNumber, correctionDateString);
        incomingTaxInvoice.Subject = string.IsNullOrEmpty(incomingTaxInvoice.Subject) ?
          correctionString :
          string.Format("{0}{1}{2}", correctionString, Environment.NewLine, incomingTaxInvoice.Subject);
        return;
      }
      
      incomingTaxInvoice.Corrected = documents.FirstOrDefault();
      
      var probability = Module.PropertyProbabilityLevels.Max / documents.Count();
      
      // Если Контрагент определен с уровнем вероятности "Ниже среднего" и ниже,
      // то для корректировочного документа использовать минимальный уровень вероятности.
      var counterpartyProbability = Commons.PublicFunctions.EntityRecognitionInfo.GetProbabilityByPropertyName(arioDocument.RecognitionInfo,
                                                                                                               props.Counterparty.Name);
      if (!counterpartyProbability.HasValue)
        return;
      if (counterpartyProbability.Value <= Module.PropertyProbabilityLevels.LowerMiddle)
        probability = Module.PropertyProbabilityLevels.Min;
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionDateFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionDateField,
                                                                        props.Corrected.Name,
                                                                        incomingTaxInvoice.Corrected,
                                                                        probability);
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionNumberFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionNumberField,
                                                                        props.Corrected.Name,
                                                                        incomingTaxInvoice.Corrected,
                                                                        probability);
    }
    
    /// <summary>
    /// Заполнить корректируемый документ в выставленном СФ.
    /// </summary>
    /// <param name="outgoingTaxInvoice">Выставленный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillOutgoingTaxInvoiceCorrectedDocument(FinancialArchive.IOutgoingTaxInvoice outgoingTaxInvoice,
                                                                IDocumentInfo documentInfo)
    {
      this.FillOutgoingTaxInvoicelCorrectedDocumentRevisionInfo(outgoingTaxInvoice, documentInfo);
      
      var arioDocument = documentInfo.ArioDocument;
      var props = outgoingTaxInvoice.Info.Properties;
      
      var correctionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionDateField)
        .FirstOrDefault();
      var correctionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(correctionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionDateField);
      
      var correctionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                                ArioGrammars.FinancialDocumentFact.Name,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionNumberField)
        .FirstOrDefault();
      var correctionNumber = Commons.PublicFunctions.Module.GetFieldValue(correctionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.CorrectionNumberField);
      
      var documents = FinancialArchive.OutgoingTaxInvoices.GetAll()
        .Where(d => d.Id != outgoingTaxInvoice.Id && d.IsAdjustment != true)
        .Where(d => d.RegistrationNumber.Equals(correctionNumber, StringComparison.InvariantCultureIgnoreCase) &&
               d.RegistrationDate == correctionDate &&
               outgoingTaxInvoice.Counterparty != null &&
               Equals(d.Counterparty, outgoingTaxInvoice.Counterparty));
      
      if (!documents.Any())
      {
        var correctionDateString = correctionDate == null ? string.Empty : correctionDate.Value.Date.ToString("d");
        var correctionString = Resources.TaxInvoiceCorrectsFormat(correctionNumber, correctionDateString);
        outgoingTaxInvoice.Subject = string.IsNullOrEmpty(outgoingTaxInvoice.Subject) ?
          correctionString :
          string.Format("{0}{1}{2}", correctionString, Environment.NewLine, outgoingTaxInvoice.Subject);
        return;
      }
      
      outgoingTaxInvoice.Corrected = documents.FirstOrDefault();
      
      var probability = Module.PropertyProbabilityLevels.Max / documents.Count();
      
      // Если Контрагент определен с уровнем вероятности "Ниже среднего" и ниже,
      // то для корректировочного документа использовать минимальный уровень вероятности.
      var counterpartyPropbability = Commons.PublicFunctions.EntityRecognitionInfo.GetProbabilityByPropertyName(arioDocument.RecognitionInfo,
                                                                                                                props.Counterparty.Name);
      if (!counterpartyPropbability.HasValue)
        return;
      if (counterpartyPropbability.Value <= Module.PropertyProbabilityLevels.LowerMiddle)
        probability = Module.PropertyProbabilityLevels.Min;
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionDateFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionDateField,
                                                                        props.Corrected.Name,
                                                                        outgoingTaxInvoice.Corrected,
                                                                        probability);
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionNumberFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionNumberField,
                                                                        props.Corrected.Name,
                                                                        outgoingTaxInvoice.Corrected,
                                                                        probability);
    }
    
    /// <summary>
    /// Заполнить корректируемый УПД.
    /// </summary>
    /// <param name="universalTransferDocument">Корректирующий УПД.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillUniversalTransferDocumentCorrectedDocument(FinancialArchive.IUniversalTransferDocument universalTransferDocument,
                                                                       IDocumentInfo documentInfo)
    {
      this.FillUniversalCorrectedDocumentRevisionInfo(universalTransferDocument, documentInfo);
      
      var arioDocument = documentInfo.ArioDocument;
      var props = universalTransferDocument.Info.Properties;
      
      var correctionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionDateField)
        .FirstOrDefault();
      var correctionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(correctionDateFact, ArioGrammars.FinancialDocumentFact.CorrectionDateField);
      
      var correctionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                                ArioGrammars.FinancialDocumentFact.Name,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionNumberField)
        .FirstOrDefault();
      
      var correctionNumber = Commons.PublicFunctions.Module.GetFieldValue(correctionNumberFact, ArioGrammars.FinancialDocumentFact.CorrectionNumberField);
      
      var documents = FinancialArchive.UniversalTransferDocuments.GetAll()
        .Where(d => d.Id != universalTransferDocument.Id && d.IsAdjustment != true)
        .Where(d => d.RegistrationNumber.Equals(correctionNumber, StringComparison.InvariantCultureIgnoreCase) &&
               d.RegistrationDate == correctionDate &&
               universalTransferDocument.Counterparty != null &&
               Equals(d.Counterparty, universalTransferDocument.Counterparty));
      
      if (!documents.Any())
      {
        var correctionDateString = correctionDate == null ? string.Empty : correctionDate.Value.Date.ToString("d");
        var correctionString = Resources.UTDCorrectsFormat(correctionNumber, correctionDateString);
        universalTransferDocument.Subject = string.IsNullOrEmpty(universalTransferDocument.Subject) ?
          correctionString :
          string.Format("{0}{1}{2}", correctionString, Environment.NewLine, universalTransferDocument.Subject);
        return;
      }
      
      universalTransferDocument.Corrected = documents.FirstOrDefault();
      
      var probability = Module.PropertyProbabilityLevels.Max / documents.Count();
      
      // Если Контрагент определен с уровнем вероятности "Ниже среднего" и ниже,
      // то для корректировочного документа использовать минимальный уровень вероятности.
      var counterpartyPropbability = Commons.PublicFunctions.EntityRecognitionInfo.GetProbabilityByPropertyName(arioDocument.RecognitionInfo,
                                                                                                                props.Counterparty.Name);
      if (!counterpartyPropbability.HasValue)
        return;
      if (counterpartyPropbability.Value <= Module.PropertyProbabilityLevels.LowerMiddle)
        probability = Module.PropertyProbabilityLevels.Min;
      
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionDateFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionDateField,
                                                                        props.Corrected.Name,
                                                                        universalTransferDocument.Corrected,
                                                                        probability);
      Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                        correctionNumberFact,
                                                                        ArioGrammars.FinancialDocumentFact.CorrectionNumberField,
                                                                        props.Corrected.Name,
                                                                        universalTransferDocument.Corrected,
                                                                        probability);
    }
    
    /// <summary>
    /// Для исправленного полученного СФ заполнить признак исправленного СФ, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="incomingTaxInvoice">Полученный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillIncomingTaxInvoiceRevisionInfo(FinancialArchive.IIncomingTaxInvoice incomingTaxInvoice,
                                                           IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.RevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.RevisionDateField).FirstOrDefault();
      
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.RevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.RevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        incomingTaxInvoice.Subject = string.IsNullOrEmpty(incomingTaxInvoice.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", incomingTaxInvoice.Subject, Environment.NewLine, revisionString);
        
        incomingTaxInvoice.IsRevision = true;
        
        // Поиск исправляемого документа для создания связи
        var originalIncomingTaxInvoice = FinancialArchive.IncomingTaxInvoices.GetAll()
          .Where(d => d.Id != incomingTaxInvoice.Id && d.IsRevision != true)
          .Where(d => d.RegistrationNumber.Equals(incomingTaxInvoice.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == incomingTaxInvoice.RegistrationDate &&
                 Equals(d.Counterparty, incomingTaxInvoice.Counterparty)).FirstOrDefault();
        
        if (originalIncomingTaxInvoice != null)
        {
          incomingTaxInvoice.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalIncomingTaxInvoice);
        }
      }
    }
    
    /// <summary>
    /// Для исправленного корректировочного полученного СФ заполнить признак исправленного СФ, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="incomingTaxInvoice">Корректировочный полученный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillIncomingTaxInvoicelCorrectedDocumentRevisionInfo(FinancialArchive.IIncomingTaxInvoice incomingTaxInvoice,
                                                                             IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField).FirstOrDefault();
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        incomingTaxInvoice.Subject = string.IsNullOrEmpty(incomingTaxInvoice.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", incomingTaxInvoice.Subject, Environment.NewLine, revisionString);
        
        incomingTaxInvoice.IsRevision = true;
        
        // Поиск исправляемого корректировочного выставленного СФ для создания связи
        var originalIncomingTaxInvoice = FinancialArchive.IncomingTaxInvoices.GetAll()
          .Where(d => d.Id != incomingTaxInvoice.Id && d.IsRevision != true && d.IsAdjustment == true)
          .Where(d => d.RegistrationNumber.Equals(incomingTaxInvoice.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == incomingTaxInvoice.RegistrationDate &&
                 Equals(d.Counterparty, incomingTaxInvoice.Counterparty)).FirstOrDefault();
        
        if (originalIncomingTaxInvoice != null)
        {
          incomingTaxInvoice.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalIncomingTaxInvoice);
        }
      }
    }
    
    /// <summary>
    /// Для исправленного выставленного СФ заполнить признак исправленного СФ, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="outgoingTaxInvoice">Выставленный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillOutgoingTaxInvoiceRevisionInfo(FinancialArchive.IOutgoingTaxInvoice outgoingTaxInvoice,
                                                           IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.RevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.RevisionDateField).FirstOrDefault();
      
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.RevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.RevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        outgoingTaxInvoice.Subject = string.IsNullOrEmpty(outgoingTaxInvoice.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", outgoingTaxInvoice.Subject, Environment.NewLine, revisionString);
        
        outgoingTaxInvoice.IsRevision = true;
        
        // Поиск исправляемого документа для создания связи
        var originalOutgoingTaxInvoice = FinancialArchive.OutgoingTaxInvoices.GetAll()
          .Where(d => d.Id != outgoingTaxInvoice.Id && d.IsRevision != true)
          .Where(d => d.RegistrationNumber.Equals(outgoingTaxInvoice.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == outgoingTaxInvoice.RegistrationDate &&
                 Equals(d.Counterparty, outgoingTaxInvoice.Counterparty)).FirstOrDefault();
        
        if (originalOutgoingTaxInvoice != null)
        {
          outgoingTaxInvoice.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalOutgoingTaxInvoice);
        }
      }
    }
    
    /// <summary>
    /// Для исправленного корректировочного выставленного СФ заполнить признак исправленного СФ, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="outgoingTaxInvoice">Корректировочный выставленный СФ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillOutgoingTaxInvoicelCorrectedDocumentRevisionInfo(FinancialArchive.IOutgoingTaxInvoice outgoingTaxInvoice,
                                                                             IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField).FirstOrDefault();
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        outgoingTaxInvoice.Subject = string.IsNullOrEmpty(outgoingTaxInvoice.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", outgoingTaxInvoice.Subject, Environment.NewLine, revisionString);
        
        outgoingTaxInvoice.IsRevision = true;
        
        // Поиск исправляемого корректировочного выставленного СФ для создания связи
        var originalOutgoingTaxInvoice = FinancialArchive.OutgoingTaxInvoices.GetAll()
          .Where(d => d.Id != outgoingTaxInvoice.Id && d.IsRevision != true && d.IsAdjustment == true)
          .Where(d => d.RegistrationNumber.Equals(outgoingTaxInvoice.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == outgoingTaxInvoice.RegistrationDate &&
                 Equals(d.Counterparty, outgoingTaxInvoice.Counterparty)).FirstOrDefault();
        
        if (originalOutgoingTaxInvoice != null)
        {
          outgoingTaxInvoice.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalOutgoingTaxInvoice);
        }
      }
    }
    
    /// <summary>
    /// Для исправленного УПД заполнить признак исправленного УПД, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="universalTransferDocument">УПД.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillUniversalTransferDocumentRevisionInfo(FinancialArchive.IUniversalTransferDocument universalTransferDocument,
                                                                  IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.RevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.RevisionDateField).FirstOrDefault();
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.RevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.RevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        universalTransferDocument.Subject = string.IsNullOrEmpty(universalTransferDocument.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", universalTransferDocument.Subject, Environment.NewLine, revisionString);
        
        universalTransferDocument.IsRevision = true;
        
        // Поиск исправляемого документа для создания связи
        var originalUniversalTransferDocument = FinancialArchive.UniversalTransferDocuments.GetAll()
          .Where(d => d.Id != universalTransferDocument.Id && d.IsRevision != true)
          .Where(d => d.RegistrationNumber.Equals(universalTransferDocument.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == universalTransferDocument.RegistrationDate &&
                 Equals(d.Counterparty, universalTransferDocument.Counterparty)).FirstOrDefault();
        
        if (originalUniversalTransferDocument != null)
        {
          universalTransferDocument.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalUniversalTransferDocument);
        }
      }
    }
    
    /// <summary>
    /// Для исправленного УКД заполнить признак исправленного УКД, в поле Содержание № и дату исправления.
    /// </summary>
    /// <param name="universalTransferDocument">УКД.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    [Public]
    public virtual void FillUniversalCorrectedDocumentRevisionInfo(FinancialArchive.IUniversalTransferDocument universalTransferDocument,
                                                                   IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      var revisionNumberFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                              ArioGrammars.FinancialDocumentFact.Name,
                                                                              ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField).FirstOrDefault();
      
      var revisionDateFact = Commons.PublicFunctions.Module.GetOrderedFacts(arioDocument.Facts,
                                                                            ArioGrammars.FinancialDocumentFact.Name,
                                                                            ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField).FirstOrDefault();
      if (revisionNumberFact != null || revisionDateFact != null)
      {
        var revisionNumber = Commons.PublicFunctions.Module.GetFieldValue(revisionNumberFact,
                                                                          ArioGrammars.FinancialDocumentFact.CorrectionRevisionNumberField);
        
        var revisionDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(revisionDateFact,
                                                                                ArioGrammars.FinancialDocumentFact.CorrectionRevisionDateField);
        var revisionDateString = revisionDate == null ? string.Empty : revisionDate.Value.Date.ToString("d");
        
        var revisionString = string.Format(Exchange.Resources.TaxInvoiceRevision, revisionNumber, revisionDateString);
        
        universalTransferDocument.Subject = string.IsNullOrEmpty(universalTransferDocument.Subject) ?
          revisionString :
          string.Format("{0}{1}{2}", universalTransferDocument.Subject, Environment.NewLine, revisionString);
        
        universalTransferDocument.IsRevision = true;
        
        // Поиск исправляемого УКД для создания связи
        var originalUniversalTransferDocument = FinancialArchive.UniversalTransferDocuments.GetAll()
          .Where(d => d.Id != universalTransferDocument.Id && d.IsRevision != true && d.IsAdjustment == true)
          .Where(d => d.RegistrationNumber.Equals(universalTransferDocument.RegistrationNumber, StringComparison.InvariantCultureIgnoreCase) &&
                 d.RegistrationDate == universalTransferDocument.RegistrationDate &&
                 Equals(d.Counterparty, universalTransferDocument.Counterparty)).FirstOrDefault();
        
        if (originalUniversalTransferDocument != null)
        {
          universalTransferDocument.Relations.Add(Exchange.PublicConstants.Module.SimpleRelationRelationName, originalUniversalTransferDocument);
        }
      }
    }
    
    /// <summary>
    /// Заполнить НОР и контрагента в финансовом документе.
    /// </summary>
    /// <param name="accountingDocument">Финансовый документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="recognizedDocumentParties">НОР и контрагент.</param>
    [Public]
    public virtual void FillAccountingDocumentParties(Docflow.IAccountingDocumentBase accountingDocument,
                                                      IDocumentInfo documentInfo,
                                                      IRecognizedDocumentParties recognizedDocumentParties)
    {
      var recognitionInfo = documentInfo.ArioDocument.RecognitionInfo;
      var props = accountingDocument.Info.Properties;
      var counterpartyPropertyName = props.Counterparty.Name;
      var businessUnitPropertyName = props.BusinessUnit.Name;
      var counterparty = recognizedDocumentParties.Counterparty;
      var businessUnit = recognizedDocumentParties.BusinessUnit;
      
      var businessUnitMatched = businessUnit != null && businessUnit.BusinessUnit != null;
      var counterpartyMatched = recognizedDocumentParties.Counterparty != null &&
        recognizedDocumentParties.Counterparty.Counterparty != null;

      accountingDocument.Counterparty = counterparty != null ? counterparty.Counterparty : null;
      accountingDocument.BusinessUnit = businessUnitMatched ? businessUnit.BusinessUnit : recognizedDocumentParties.ResponsibleEmployeeBusinessUnit;

      if (counterpartyMatched)
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, recognizedDocumentParties.Counterparty.Fact, null,
                                                                          counterpartyPropertyName, recognizedDocumentParties.Counterparty.Counterparty,
                                                                          recognizedDocumentParties.Counterparty.CounterpartyProbability);

      if (businessUnitMatched)
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, recognizedDocumentParties.BusinessUnit.Fact, null,
                                                                          businessUnitPropertyName, recognizedDocumentParties.BusinessUnit.BusinessUnit,
                                                                          recognizedDocumentParties.BusinessUnit.BusinessUnitProbability);
      else
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(recognitionInfo, null, null,
                                                                          businessUnitPropertyName, recognizedDocumentParties.ResponsibleEmployeeBusinessUnit,
                                                                          Module.PropertyProbabilityLevels.Min);
    }
    #endregion
    
    #region Документооборот

    /// <summary>
    /// Заполнить свойства по результатам обработки Ario.
    /// </summary>
    /// <param name="simpleDocument">Простой документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    /// <param name="documentName">Имя документа.</param>
    [Public]
    public virtual void FillSimpleDocumentProperties(Docflow.ISimpleDocument simpleDocument,
                                                     IDocumentInfo documentInfo,
                                                     Sungero.Company.IEmployee responsible,
                                                     string documentName)
    {
      // Вид документа.
      this.FillDocumentKind(simpleDocument);
      
      simpleDocument.Name = documentName;
      simpleDocument.PreparedBy = responsible;
      simpleDocument.BusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);
      simpleDocument.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
    }
    
    #endregion
    
    #region Получение даты и номера
    
    /// <summary>
    /// Распознать дату документа.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="factName">Наименование факта.</param>
    /// <param name="fieldName">Наименование поля.</param>
    /// <returns>Результаты распознавания даты.</returns>
    [Public]
    public virtual IRecognizedDocumentDate GetRecognizedDate(List<IArioFact> facts,
                                                             string factName,
                                                             string fieldName)
    {
      var defaultProbability = Module.PropertyProbabilityLevels.Min;
      var defaultDate = Calendar.SqlMinValue;
      var dateFact = Commons.PublicFunctions.Module.GetOrderedFacts(facts, factName, fieldName).FirstOrDefault();
      var recognizedDate = RecognizedDocumentDate.Create(null, null, null);
      
      if (dateFact == null)
        return recognizedDate;
      
      recognizedDate.Fact = dateFact;
      recognizedDate.Date = defaultDate;
      recognizedDate.Probability = defaultProbability;
      var date = Commons.PublicFunctions.Module.GetFieldDateTimeValue(dateFact, fieldName);
      var isDateValid = date == null ||
        date != null && date.HasValue && date >= Calendar.SqlMinValue;
      
      if (isDateValid)
      {
        recognizedDate.Date = date;
        recognizedDate.Probability = Commons.PublicFunctions.Module.GetFieldProbability(dateFact, fieldName);
      }
      
      return recognizedDate;
    }
    
    /// <summary>
    /// Распознать номер документа.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="factName">Наименование факта.</param>
    /// <param name="fieldName">Наименование поля.</param>
    /// <param name="numberPropertyInfo">Информация о свойстве с номером.</param>
    /// <returns>Результаты распознавания номера.</returns>
    [Public]
    public virtual IRecognizedDocumentNumber GetRecognizedNumber(List<IArioFact> facts,
                                                                 string factName, string fieldName,
                                                                 Sungero.Domain.Shared.IStringPropertyInfo numberPropertyInfo)
    {
      var recognizedNumber = RecognizedDocumentNumber.Create();
      var numberFact = Commons.PublicFunctions.Module.GetOrderedFacts(facts, factName, fieldName).FirstOrDefault();
      
      if (numberFact == null)
        return recognizedNumber;
      
      recognizedNumber.Fact = numberFact;
      
      var number = Commons.PublicFunctions.Module.GetFieldValue(numberFact, fieldName);
      if (string.IsNullOrWhiteSpace(number))
        return recognizedNumber;
      
      recognizedNumber.Number = number;
      recognizedNumber.Probability = Commons.PublicFunctions.Module.GetFieldProbability(numberFact, fieldName);
      if (number.Length > numberPropertyInfo.Length)
      {
        recognizedNumber.Number = number.Substring(0, numberPropertyInfo.Length);
        recognizedNumber.Probability = Module.PropertyProbabilityLevels.LowerMiddle;
      }
      
      return recognizedNumber;
    }
    
    #endregion
    
    #region Получение суммы и валюты
    
    /// <summary>
    /// Распознать сумму.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <returns>Результаты распознавания суммы.</returns>
    [Public]
    public virtual IRecognizedAmount GetRecognizedAmount(List<IArioFact> facts)
    {
      var recognizedAmount = RecognizedAmount.Create();
      var amountFacts = Commons.PublicFunctions.Module.GetOrderedFacts(facts,
                                                                       ArioGrammars.DocumentAmountFact.Name,
                                                                       ArioGrammars.DocumentAmountFact.AmountField);
      
      var amountFact = amountFacts.FirstOrDefault();
      if (amountFact == null)
        return recognizedAmount;
      
      recognizedAmount.Fact = amountFact;
      
      var totalAmount = Commons.PublicFunctions.Module.GetFieldNumericalValue(amountFact,
                                                                              ArioGrammars.DocumentAmountFact.AmountField);
      if (!totalAmount.HasValue || totalAmount.Value <= 0)
        return recognizedAmount;
      
      recognizedAmount.HasValue = true;
      recognizedAmount.Amount = totalAmount.Value;
      recognizedAmount.Probability = Commons.PublicFunctions.Module.GetFieldProbability(amountFact,
                                                                                        ArioGrammars.DocumentAmountFact.AmountField);
      
      // Если в сумме больше 2 знаков после запятой, то обрезать лишние разряды,
      // иначе пометить, что результату извлечения можно доверять.
      var roundedAmount = Math.Round(totalAmount.Value, 2);
      var amountClipping = roundedAmount - totalAmount.Value != 0.0;
      if (amountClipping)
      {
        recognizedAmount.Amount = roundedAmount;
        recognizedAmount.Probability = Module.PropertyProbabilityLevels.UpperMiddle;
      }
      
      return recognizedAmount;
    }
    
    /// <summary>
    /// Распознать валюту.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <returns>Результаты распознавания валюты.</returns>
    [Public]
    public virtual IRecognizedCurrency GetRecognizedCurrency(List<IArioFact> facts)
    {
      var recognizedCurrency = RecognizedCurrency.Create();
      
      var currencyFact = Commons.PublicFunctions.Module.GetOrderedFacts(facts,
                                                                        ArioGrammars.DocumentAmountFact.Name,
                                                                        ArioGrammars.DocumentAmountFact.CurrencyField)
        .FirstOrDefault();
      if (currencyFact == null)
        return recognizedCurrency;
      
      var currencyCode = Commons.PublicFunctions.Module.GetFieldValue(currencyFact,
                                                                      ArioGrammars.DocumentAmountFact.CurrencyField);
      int resCurrencyCode;
      if (!int.TryParse(currencyCode, out resCurrencyCode))
        return recognizedCurrency;
      
      var currency = Commons.Currencies.GetAll(x => x.NumericCode == currencyCode).OrderBy(x => x.Status).FirstOrDefault();
      if ((currency == null) || (currency.Status == CoreEntities.DatabookEntry.Status.Closed))
        return recognizedCurrency;
      
      recognizedCurrency.HasValue = true;
      recognizedCurrency.Probability = Commons.PublicFunctions.Module.GetFieldProbability(currencyFact,
                                                                                          ArioGrammars.DocumentAmountFact.CurrencyField);
      recognizedCurrency.Currency = currency;
      recognizedCurrency.Fact = currencyFact;
      
      return recognizedCurrency;
    }
    
    #endregion
    
    #region Получение ведущего документа
    
    /// <summary>
    /// Получить ведущий договорной документ по номеру и дате из факта.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="leadingDocPropertyName">Имя связанного свойства.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyPropertyName">Имя свойства, связанного с контрагентом.</param>
    /// <returns>Структура, содержащая ведущий договорной документ, факт и признак доверия.</returns>
    [Public]
    public virtual IRecognizedContract GetLeadingContractualDocument(IArioFact fact,
                                                                     string leadingDocPropertyName,
                                                                     Parties.ICounterparty counterparty,
                                                                     string counterpartyPropertyName)
    {
      var result = RecognizedContract.Create(Contracts.ContractualDocuments.Null, fact, null);
      if (fact == null)
        return result;

      if (!string.IsNullOrEmpty(leadingDocPropertyName) && counterparty != null)
      {
        result = this.GetPreviousContractRecognitionResults(fact, leadingDocPropertyName, counterparty.Id.ToString(),
                                                            counterpartyPropertyName);
        if (result.Contract != null)
          return result;
      }
      
      if (fact == null)
        return result;
      
      var docDate = Commons.PublicFunctions.Module.GetFieldDateTimeValue(fact, ArioGrammars.FinancialDocumentFact.DocumentBaseDateField);
      var number = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.FinancialDocumentFact.DocumentBaseNumberField);
      
      if (string.IsNullOrWhiteSpace(number))
        return result;
      
      var contracts = Contracts.ContractualDocuments.GetAll(x => x.RegistrationNumber == number &&
                                                            x.RegistrationDate == docDate &&
                                                            (counterparty == null || x.Counterparty.Equals(counterparty)));
      
      result.Contract = contracts.FirstOrDefault();
      if (contracts.Count() == 1)
        result.Probability = Module.PropertyProbabilityLevels.Max;
      
      if (contracts.Count() > 1)
        result.Probability = Module.PropertyProbabilityLevels.LowerMiddle;
      
      return result;
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания договорного документа по факту, идентичному переданному,
    /// с фильтрацией по контрагенту.
    /// </summary>
    /// <param name="fact">Факт Арио.</param>
    /// <param name="propertyName">Имя связанного свойства.</param>
    /// <param name="filterPropertyValue">Значение свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <param name="filterPropertyName">Имя свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <returns>Структура, содержащая договорной документ, подтвержденный пользователем, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedContract GetPreviousContractRecognitionResults(IArioFact fact,
                                                                             string propertyName,
                                                                             string filterPropertyValue,
                                                                             string filterPropertyName)
    {
      var result = RecognizedContract.Create(Contracts.ContractualDocuments.Null, fact, null);
      var contractRecognitionInfo = Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName,
                                                                                                         filterPropertyValue,
                                                                                                         filterPropertyName);
      if (contractRecognitionInfo == null)
        return result;
      
      int docId;
      if (!int.TryParse(contractRecognitionInfo.VerifiedValue, out docId))
        return result;
      
      var contract = Contracts.ContractualDocuments.GetAll(x => x.Id == docId).FirstOrDefault();
      if (contract != null)
      {
        result.Contract = contract;
        result.Probability = contractRecognitionInfo.Probability;
      }
      return result;
    }

    #endregion
    
    #region Получение контрагентов, контактов, подписантов, НОР
    
    /// <summary>
    /// Определить направление документа, НОР и КА у счет-фактуры.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат подбора сторон сделки для документа.</returns>
    /// <remarks>Если НОР выступает продавцом, то счет-фактура - исходящая, иначе - входящая.</remarks>
    [Public]
    public virtual IRecognizedDocumentParties GetRecognizedTaxInvoiceParties(List<IArioFact> facts,
                                                                             IEmployee responsible)
    {
      var responsibleEmployeeBusinessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(responsible);
      var responsibleEmployeePersonalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(responsible);
      var responsibleEmployeePersonalSettingsBusinessUnit = responsibleEmployeePersonalSettings != null
        ? responsibleEmployeePersonalSettings.BusinessUnit
        : Company.BusinessUnits.Null;
      var document = AccountingDocumentBases.Null;
      var props = AccountingDocumentBases.Info.Properties;

      // Определить направление документа, НОР и КА.
      // Если НОР выступает продавцом, то создаем исходящую счет-фактуру, иначе - входящую.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);

      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterparties(facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee).FirstOrDefault();

      var buyerIsBusinessUnit = buyer != null && buyer.BusinessUnit != null;
      var sellerIsBusinessUnit = seller != null && seller.BusinessUnit != null;
      var recognizedDocumentParties = RecognizedDocumentParties.Create();
      if (buyerIsBusinessUnit && sellerIsBusinessUnit)
      {
        // Мультинорность. Уточнить НОР по ответственному.
        if (Equals(seller.BusinessUnit, responsibleEmployeePersonalSettingsBusinessUnit) ||
            Equals(seller.BusinessUnit, responsibleEmployeeBusinessUnit))
        {
          // Исходящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = true;
          recognizedDocumentParties.Counterparty = buyer;
          recognizedDocumentParties.BusinessUnit = seller;
        }
        else
        {
          // Входящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = false;
          recognizedDocumentParties.Counterparty = seller;
          recognizedDocumentParties.BusinessUnit = buyer;
        }
      }
      else if (buyerIsBusinessUnit)
      {
        // Входящий документ.
        recognizedDocumentParties.IsDocumentOutgoing = false;
        recognizedDocumentParties.Counterparty = seller;
        recognizedDocumentParties.BusinessUnit = buyer;
      }
      else if (sellerIsBusinessUnit)
      {
        // Исходящий документ.
        recognizedDocumentParties.IsDocumentOutgoing = true;
        recognizedDocumentParties.Counterparty = buyer;
        recognizedDocumentParties.BusinessUnit = seller;
      }
      else
      {
        // НОР не найдена по фактам - НОР будет взята по ответственному.
        if (buyer != null && buyer.Counterparty != null && (seller == null || seller.Counterparty == null))
        {
          // Исходящий документ, потому что buyer - контрагент, а другой информации нет.
          recognizedDocumentParties.IsDocumentOutgoing = true;
          recognizedDocumentParties.Counterparty = buyer;
        }
        else
        {
          // Входящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = false;
          recognizedDocumentParties.Counterparty = seller;
        }
      }

      recognizedDocumentParties.ResponsibleEmployeeBusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);

      return recognizedDocumentParties;
    }

    /// <summary>
    /// Подобрать участников сделки (НОР и контрагент).
    /// </summary>
    /// <param name="buyer">Список фактов с данными о контрагенте. Тип контрагента - покупатель.</param>
    /// <param name="seller">Список фактов с данными о контрагенте. Тип контрагента - продавец.</param>
    /// <param name="responsibleEmployee">Ответственный сотрудник.</param>
    /// <returns>НОР и контрагент.</returns>
    [Public]
    public virtual IRecognizedDocumentParties GetDocumentParties(IRecognizedCounterparty buyer,
                                                                 IRecognizedCounterparty seller,
                                                                 Company.IEmployee responsibleEmployee)
    {
      IRecognizedCounterparty counterparty = null;
      IRecognizedCounterparty businessUnit = null;

      // НОР.
      if (buyer != null)
        businessUnit = buyer;

      // Контрагент.
      if (seller != null && seller.Counterparty != null)
        counterparty = seller;

      var responsibleEmployeeBusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsibleEmployee);

      return RecognizedDocumentParties.Create(businessUnit, counterparty, responsibleEmployeeBusinessUnit, null);
    }

    /// <summary>
    /// Подобрать участников сделки (НОР и контрагент).
    /// </summary>
    /// <param name="buyer">Список фактов с данными о контрагенте. Тип контрагента - покупатель.</param>
    /// <param name="seller">Список фактов с данными о контрагенте. Тип контрагента - продавец.</param>
    /// <param name="nonType">Список фактов с данными о контрагенте. Тип контрагента не заполнен.</param>
    /// <param name="responsibleEmployee">Ответственный сотрудник.</param>
    /// <returns>НОР и контрагент.</returns>
    [Public]
    public virtual IRecognizedDocumentParties GetDocumentParties(IRecognizedCounterparty buyer,
                                                                 IRecognizedCounterparty seller,
                                                                 List<IRecognizedCounterparty> nonType,
                                                                 Company.IEmployee responsibleEmployee)
    {
      IRecognizedCounterparty counterparty = null;
      IRecognizedCounterparty businessUnit = null;
      var responsibleEmployeeBusinessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(responsibleEmployee);
      var responsibleEmployeePersonalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(responsibleEmployee);
      var responsibleEmployeePersonalSettingsBusinessUnit = responsibleEmployeePersonalSettings != null
        ? responsibleEmployeePersonalSettings.BusinessUnit
        : Company.BusinessUnits.Null;

      // НОР.
      if (buyer != null)
      {
        // НОР по факту с типом BUYER.
        businessUnit = buyer;
      }
      else
      {
        // НОР по факту без типа.
        var nonTypeBusinessUnits = nonType.Where(m => m.BusinessUnit != null);

        // Уточнить НОР по ответственному.
        if (nonTypeBusinessUnits.Count() > 1)
        {
          // Если в персональных настройках ответственного указана НОР.
          if (responsibleEmployeePersonalSettingsBusinessUnit != null)
            businessUnit = nonTypeBusinessUnits.Where(m => Equals(m.BusinessUnit, responsibleEmployeePersonalSettingsBusinessUnit)).FirstOrDefault();

          // НОР не определилась по персональным настройкам ответственного.
          if (businessUnit == null)
            businessUnit = nonTypeBusinessUnits.Where(m => Equals(m.BusinessUnit, responsibleEmployeeBusinessUnit)).FirstOrDefault();
        }
        // Если не удалось уточнить НОР по ответственному, то берем первую наиболее вероятную.
        if (businessUnit == null)
          businessUnit = nonTypeBusinessUnits.OrderByDescending(x => x.BusinessUnitProbability).FirstOrDefault();
      }

      // Контрагент по типу.
      if (seller != null && seller.Counterparty != null)
      {
        // Контрагент по факту с типом SELLER.
        counterparty = seller;
      }
      else
      {
        // Берем наиболее вероятного контрагента по факту без типа. Исключить факт, по которому нашли НОР.
        var nonTypeCounterparties = nonType
          .Where(m => m.Counterparty != null)
          .Where(m => !Equals(m, businessUnit));
        counterparty = nonTypeCounterparties.OrderByDescending(x => x.CounterpartyProbability).FirstOrDefault();
      }

      // В качестве НОР ответственного вернуть НОР из персональных настроек, если она указана.
      return responsibleEmployeePersonalSettingsBusinessUnit != null
        ? RecognizedDocumentParties.Create(businessUnit, counterparty, responsibleEmployeePersonalSettingsBusinessUnit, null)
        : RecognizedDocumentParties.Create(businessUnit, counterparty, responsibleEmployeeBusinessUnit, null);
    }

    /// <summary>
    /// Подобрать по факту контрагента и НОР.
    /// </summary>
    /// <param name="allFacts">Факты.</param>
    /// <param name="arioCounterpartyTypes">Типы фактов контрагентов.</param>
    /// <returns>Наши организации и контрагенты, найденные по фактам.</returns>
    [Public]
    public virtual List<IRecognizedCounterparty> GetRecognizedAccountingDocumentCounterparties(List<IArioFact> allFacts,
                                                                                               List<string> arioCounterpartyTypes)
    {
      var props = AccountingDocumentBases.Info.Properties;
      var recognizedCounterparties = new List<IRecognizedCounterparty>();
      var facts = allFacts.Where(f => f.Name == ArioGrammars.CounterpartyFact.Name);
      
      foreach (var fact in facts)
      {
        var businessUnit = Company.BusinessUnits.Null;
        var counterparty = Parties.Counterparties.Null;
        double? businessUnitProbability = 0.0;
        double? counterpartyProbability = 0.0;

        // Если для свойства BusinessUnit по факту существует верифицированное ранее значение, то вернуть его.
        var verifiedBusinessUnit = this.GetPreviousBusinessUnitRecognitionResults(fact, props.BusinessUnit.Name);
        if (verifiedBusinessUnit != null)
        {
          businessUnit = verifiedBusinessUnit.BusinessUnit;
          businessUnitProbability = verifiedBusinessUnit.BusinessUnitProbability;
        }

        // Если для свойства Counterparty по факту существует верифицированное ранее значение, то вернуть его.
        var verifiedCounterparty = this.GetPreviousCounterpartyRecognitionResults(fact, props.Counterparty.Name);
        if (verifiedCounterparty != null)
        {
          counterparty = verifiedCounterparty.Counterparty;
          counterpartyProbability = verifiedCounterparty.CounterpartyProbability;
        }

        // Поиск по ИНН/КПП.
        var tin = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TinField);
        var trrc = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TrrcField);
        
        // Получить обобщенную вероятность по полям ИНН, КПП.
        var tinTrrcFieldsProbability = this.GetTinTrrcFieldsProbability(fact,
                                                                        ArioGrammars.CounterpartyFact.TinField,
                                                                        ArioGrammars.CounterpartyFact.TrrcField);
        if (businessUnit == null)
        {
          var businessUnits = Company.PublicFunctions.BusinessUnit.GetBusinessUnits(tin, trrc);
          
          // Если подобрано несколько организаций, то вероятность равномерно делится между всеми.
          businessUnitProbability = businessUnits.Any() ? tinTrrcFieldsProbability / businessUnits.Count() : 0.0;
          businessUnit = businessUnits.FirstOrDefault();
        }
        if (counterparty == null)
        {
          var counterparties = Parties.PublicFunctions.Counterparty.GetDuplicateCounterparties(tin, trrc, string.Empty, true);
          
          // Если подобрано несколько организаций, то вероятность равномерно делится между всеми.
          counterpartyProbability = counterparties.Any() ? tinTrrcFieldsProbability / counterparties.Count() : 0.0;

          // Получить запись по точному совпадению по ИНН/КПП.
          if (!string.IsNullOrWhiteSpace(trrc))
            counterparty = counterparties.FirstOrDefault(x => Parties.CompanyBases.Is(x) && Parties.CompanyBases.As(x).TRRC == trrc);
          
          // Получить запись с совпадением по ИНН, если не найдено по точному совпадению ИНН/КПП.
          if (counterparty == null)
            counterparty = counterparties.FirstOrDefault();
        }

        if (counterparty != null || businessUnit != null)
        {
          var recognizedCounterparty = RecognizedCounterparty.Create(businessUnit, counterparty, fact,
                                                                     Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.CounterpartyTypeField),
                                                                     businessUnitProbability, counterpartyProbability);
          recognizedCounterparties.Add(recognizedCounterparty);
          continue;
        }

        // Если не нашли по ИНН/КПП, то ищем по наименованию.
        var name = GetCounterpartyName(fact, ArioGrammars.CounterpartyFact.NameField,
                                       ArioGrammars.CounterpartyFact.LegalFormField);
        
        var counterpartiesByName = Parties.Counterparties.GetAll()
          .Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed &&
                 x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        
        var businessUnitsByName = Company.BusinessUnits.GetAll()
          .Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed &&
                 x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        
        if (counterpartiesByName.Any() || businessUnitsByName.Any())
        {
          // Получить обобщенную вероятность по полям Наименование и ОПФ.
          var nameFieldsProbability = this.GetNameFieldsProbability(fact, ArioGrammars.CounterpartyFact.NameField,
                                                                    ArioGrammars.CounterpartyFact.LegalFormField);
          
          // Если для одного факта подобрано несколько организаций,
          // то вероятность того, что организация подобрана корректно, равномерно делится между всеми.
          businessUnitProbability = businessUnitsByName.Any() ? nameFieldsProbability / businessUnitsByName.Count() : 0.0;
          counterpartyProbability = counterpartiesByName.Any() ? nameFieldsProbability / counterpartiesByName.Count() : 0.0;
          
          var recognizedCounterparty = RecognizedCounterparty.Create(businessUnitsByName.FirstOrDefault(),
                                                                     counterpartiesByName.FirstOrDefault(), fact,
                                                                     Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.CounterpartyTypeField),
                                                                     businessUnitProbability, counterpartyProbability);
          recognizedCounterparties.Add(recognizedCounterparty);
        }
      }

      return recognizedCounterparties;
    }

    /// <summary>
    /// Поиск контрагента по извлеченным фактам.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="propertyName">Имя свойства.</param>
    /// <param name="counterpartyFactName">Наименование факта с контрагентом.</param>
    /// <param name="counterpartyNameField">Поле с наименованием контрагента.</param>
    /// <param name="counterpartyLegalFormField">Поле с юридической формой контрагента.</param>
    /// <returns>Контрагент.</returns>
    /// <remarks>Метод возвращает первое найденное по фактам верифицированное значение.
    /// Метод возвращает первого найденного по наименованию контрагента, если нет фактов, содержащих поле ИНН.
    /// Метод возвращает контрагента, подобранного по ИНН/КПП, если он подобран один.
    /// Метод возвращает первого из подобранных по имени с пустыми ИНН и КПП, если по ИНН/КПП не подобралось ни одного контрагента.
    /// Результат уточняется по наименованию, если по ИНН/КПП подобралось более одного контрагента.
    /// - Если ни один не подходит по имени, то возвращается первый подходящий по ИНН/КПП с минимальной вероятностью.
    /// - Если по имени подходит один, то он и возвращается.
    /// - Если по имени подходит несколько, то возвращается первый с минимальной вероятностью.</remarks>
    [Public]
    public virtual IRecognizedCounterparty GetRecognizedCounterparty(List<IArioFact> facts,
                                                                     string propertyName,
                                                                     string counterpartyFactName,
                                                                     string counterpartyNameField,
                                                                     string counterpartyLegalFormField)
    {
      var actualCounterparties = Sungero.Parties.Counterparties.GetAll()
        .Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);

      // Получить все факты контрагентов, содержащие наименование (counterpartyNameField),
      // отсортированные по уменьшению Probability поля факта.
      var foundByName = new List<IRecognizedCounterparty>();
      var counterpartyNames = new List<string>();
      var counterpartyNameFacts = Commons.PublicFunctions.Module.GetFacts(facts,
                                                                          counterpartyFactName,
                                                                          counterpartyNameField)
        .OrderByDescending(x => x.Fields.First(f => f.Name == counterpartyNameField).Probability);
      
      // Подобрать контрагентов, подходящих по имени для переданных фактов.
      var nameFieldsProbability = 0.0;
      foreach (var fact in counterpartyNameFacts)
      {
        // Если для свойства propertyName по факту существует верифицированное ранее значение, то вернуть его.
        // Вероятность соответствует probability верифицированного ранее значения.
        var verifiedCounterparty = this.GetPreviousCounterpartyRecognitionResults(fact, propertyName);
        if (verifiedCounterparty != null)
          return verifiedCounterparty;
        
        var name = GetCounterpartyName(fact, counterpartyNameField, counterpartyLegalFormField);
        counterpartyNames.Add(name);
        
        // Получить обобщенную вероятность по полям Наименование и ОПФ.
        nameFieldsProbability = this.GetNameFieldsProbability(fact, counterpartyNameField, counterpartyLegalFormField);
        
        var counterparties = actualCounterparties.Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        foreach (var counterparty in counterparties)
        {
          var recognizedCounterparty = RecognizedCounterparty.Create();
          recognizedCounterparty.Counterparty = counterparty;
          recognizedCounterparty.Fact = fact;
          // Если для одного факта подобрано несколько контрагентов,
          // то вероятность того, что контрагент подобран корректно, равномерно делится между всеми.
          recognizedCounterparty.CounterpartyProbability = nameFieldsProbability / counterparties.Count();
          foundByName.Add(recognizedCounterparty);
        }
      }

      // Поиск по ИНН/КПП.
      // Отфильтровать факты, у которых TinIsValid = "false".
      var counterpartyTINFacts = Commons.PublicFunctions.Module.GetFacts(facts,
                                                                         ArioGrammars.CounterpartyFact.Name,
                                                                         ArioGrammars.CounterpartyFact.TinField)
        .Where(x => Commons.PublicFunctions.Module.GetFieldValue(x, ArioGrammars.CounterpartyFact.TinIsValidField) == "true")
        .OrderByDescending(x => x.Fields.First(f => f.Name == ArioGrammars.CounterpartyFact.TinField).Probability);

      // Если нет фактов, содержащих поле ИНН, то вернуть первого контрагента по наименованию.
      if (!counterpartyTINFacts.Any())
        return foundByName.FirstOrDefault();
      
      var foundByTin = new List<IRecognizedCounterparty>();
      foreach (var fact in counterpartyTINFacts)
      {
        // Если для свойства propertyName по факту существует верифицированное ранее значение, то вернуть его.
        // Вероятность соответствует probability верифицированного ранее значения.
        var verifiedCounterparty = this.GetPreviousCounterpartyRecognitionResults(fact, propertyName);
        if (verifiedCounterparty != null)
          return verifiedCounterparty;
        
        var tinFieldValue = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TinField);
        var trrcFieldValue = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TrrcField);
        var counterparties = Parties.PublicFunctions.Counterparty.GetDuplicateCounterparties(tinFieldValue,
                                                                                             trrcFieldValue,
                                                                                             string.Empty,
                                                                                             true);
        var tinTrrcFieldsProbability = this.GetTinTrrcFieldsProbability(fact,
                                                                        ArioGrammars.CounterpartyFact.TinField,
                                                                        ArioGrammars.CounterpartyFact.TrrcField);
        foreach (var counterparty in counterparties)
        {
          var recognizedCounterparty = RecognizedCounterparty.Create();
          recognizedCounterparty.Counterparty = counterparty;
          recognizedCounterparty.Fact = fact;
          // Если для одного факта подобрано несколько контрагентов,
          // то вероятность того, что контрагент подобран корректно, равномерно делится между всеми.
          recognizedCounterparty.CounterpartyProbability = tinTrrcFieldsProbability / counterparties.Count();
          foundByTin.Add(recognizedCounterparty);
        }
      }

      // Не найдены контрагенты по ИНН\КПП. Искать по наименованию в контрагентах с пустыми ИНН/КПП.
      if (foundByTin.Count == 0)
        return foundByName
          .Where(x => string.IsNullOrEmpty(x.Counterparty.TIN))
          .Where(x => !Sungero.Parties.CompanyBases.Is(x.Counterparty) ||
                 string.IsNullOrEmpty(Sungero.Parties.CompanyBases.As(x.Counterparty).TRRC))
          .FirstOrDefault();

      // Найден один контрагент по ИНН\КПП.
      if (foundByTin.Count == 1)
        return foundByTin.First();

      // Найдено несколько контрагентов по ИНН\КПП. Уточнить поиск по наименованию.
      var specifiedByName = foundByTin
        .Where(x => counterpartyNames.Any(name => x.Counterparty.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
        .ToList();
      
      if (specifiedByName.Count == 0)
        return foundByTin.FirstOrDefault();
      
      // Если при дополнительном поиске по наименованию подобран один или более контрагентов,
      var resultCounterparty = specifiedByName.FirstOrDefault();
      var tinTrrcFact = resultCounterparty.Fact;
      // Найти факт с наименованием и ОПФ, если эти поля не содержит факт с ИНН и КПП.
      IArioFact nameLegalFormFact = null;
      if (Commons.PublicFunctions.Module.GetField(tinTrrcFact, counterpartyNameField) == null)
      {
        var recognizedByName = foundByName.FirstOrDefault(x => Equals(x.Counterparty, resultCounterparty.Counterparty));
        if (recognizedByName != null)
          nameLegalFormFact = recognizedByName.Fact;
      }
      else
      {
        nameLegalFormFact = tinTrrcFact;
      }
      
      var aggregateFieldsProbability = this.GetNameTinTrrcFieldsProbability(tinTrrcFact,
                                                                            nameLegalFormFact,
                                                                            ArioGrammars.CounterpartyFact.TinField,
                                                                            ArioGrammars.CounterpartyFact.TrrcField,
                                                                            counterpartyNameField,
                                                                            counterpartyLegalFormField);
      
      // Если для одного факта подобрано несколько контрагентов,
      // то вероятность того, что контрагент подобран корректно, равномерно делится между всеми.
      resultCounterparty.CounterpartyProbability = aggregateFieldsProbability / specifiedByName.Count();
      return resultCounterparty;
    }

    /// <summary>
    /// Получение списка НОР по извлеченным фактам.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="counterpartyFactName">Наименование факта с контрагентом.</param>
    /// <param name="counterpartyNameField">Поле с наименованием контрагента.</param>
    /// <param name="counterpartyLegalFormField">Поле с юридической формой контрагента.</param>
    /// <returns>Список НОР и соответствующих им фактов.</returns>
    [Public]
    public virtual List<IRecognizedCounterparty> GetRecognizedBusinessUnits(List<IArioFact> facts,
                                                                            string counterpartyFactName,
                                                                            string counterpartyNameField,
                                                                            string counterpartyLegalFormField)
    {
      // Получить факты с наименованиями организаций.
      var businessUnitsByName = new List<IRecognizedCounterparty>();
      var nameFieldsProbability = 0.0;
      var correspondentNameFacts = Commons.PublicFunctions.Module.GetFacts(facts, counterpartyFactName, counterpartyNameField)
        .OrderByDescending(x => x.Fields.First(f => f.Name == counterpartyNameField).Probability);
      
      foreach (var fact in correspondentNameFacts)
      {
        var name = GetCounterpartyName(fact, counterpartyNameField, counterpartyLegalFormField);
        
        // Получить обобщенную вероятность по полям Наименование и ОПФ.
        nameFieldsProbability = this.GetNameFieldsProbability(fact,
                                                              counterpartyNameField,
                                                              counterpartyLegalFormField);
        
        var businessUnits = BusinessUnits.GetAll()
          .Where(x => x.Status != CoreEntities.DatabookEntry.Status.Closed)
          .Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        
        foreach (var businessUnit in businessUnits)
        {
          var recognizedBusinessUnit = RecognizedCounterparty.Create();
          recognizedBusinessUnit.BusinessUnit = businessUnit;
          recognizedBusinessUnit.Fact = fact;
          recognizedBusinessUnit.BusinessUnitProbability = nameFieldsProbability / businessUnits.Count();
          
          businessUnitsByName.Add(recognizedBusinessUnit);
        }
      }
      
      // Если факты с ИНН/КПП не найдены, то вернуть факты с наименованиями организаций.
      // Отфильтровать факты, у которых TinIsValid = "false".
      var correspondentTinFacts = Commons.PublicFunctions.Module.GetFacts(facts,
                                                                          ArioGrammars.CounterpartyFact.Name,
                                                                          ArioGrammars.CounterpartyFact.TinField)
        .Where(x => Commons.PublicFunctions.Module.GetFieldValue(x, ArioGrammars.CounterpartyFact.TinIsValidField) == "true")
        .OrderByDescending(x => x.Fields.First(f => f.Name == ArioGrammars.CounterpartyFact.TinField).Probability);
      
      if (!correspondentTinFacts.Any())
        return businessUnitsByName;

      // Поиск по ИНН/КПП.
      var businessUnitsByTin = new List<IRecognizedCounterparty>();
      foreach (var fact in correspondentTinFacts)
      {
        var tinFieldValue = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TinField);
        var trrcFieldValue = Commons.PublicFunctions.Module.GetFieldValue(fact, ArioGrammars.CounterpartyFact.TrrcField);
        var businessUnits = Company.PublicFunctions.BusinessUnit.GetBusinessUnits(tinFieldValue, trrcFieldValue);
        var tinTrrcFieldsProbability = this.GetTinTrrcFieldsProbability(fact,
                                                                        ArioGrammars.CounterpartyFact.TinField,
                                                                        ArioGrammars.CounterpartyFact.TrrcField);
        
        foreach (var businessUnit in businessUnits)
        {
          var recognizedBusinessUnit = RecognizedCounterparty.Create();
          recognizedBusinessUnit.BusinessUnit = businessUnit;
          recognizedBusinessUnit.Fact = fact;
          recognizedBusinessUnit.BusinessUnitProbability = tinTrrcFieldsProbability / businessUnits.Count();
          
          businessUnitsByTin.Add(recognizedBusinessUnit);
        }
      }
      
      // Найдено по ИНН/КПП.
      if (businessUnitsByTin.Any())
        return businessUnitsByTin;
      
      // Если по ИНН/КПП не найдено НОР, то искать по наименованию в НОР с пустыми ИНН/КПП.
      if (businessUnitsByName.Any())
        return businessUnitsByName.Where(x => string.IsNullOrEmpty(x.BusinessUnit.TIN) && string.IsNullOrEmpty(x.BusinessUnit.TRRC)).ToList();
      
      return businessUnitsByName;
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям Наименование и ОПФ.
    /// </summary>
    /// <param name="fact">Факт с наименованием и ОПФ организации.</param>
    /// <param name="counterpartyNameField">Наименование поля с наименованием организации.</param>
    /// <param name="counterpartyLegalFormField">Наименование поля с ОПФ организации.</param>
    /// <returns>Вероятность.</returns>
    [Public]
    public virtual double GetNameFieldsProbability(IArioFact fact,
                                                   string counterpartyNameField,
                                                   string counterpartyLegalFormField)
    {
      // Получить обобщенную вероятность по полям Наименование и ОПФ.
      // Вес вероятности наименования - 0.8, вес вероятности ОПФ - 0.2.
      var weightedFields = new Dictionary<IArioFactField, double>();
      var nameField = Commons.PublicFunctions.Module.GetField(fact, counterpartyNameField);
      if (nameField != null)
        weightedFields.Add(nameField, 0.8);
      var legalFormField = Commons.PublicFunctions.Module.GetField(fact, counterpartyLegalFormField);
      if (legalFormField != null)
        weightedFields.Add(legalFormField, 0.2);
      
      return Commons.PublicFunctions.Module.GetAggregateFieldsProbability(weightedFields);
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям ИНН и КПП.
    /// </summary>
    /// <param name="fact">Факт с ИНН и КПП организации.</param>
    /// <param name="tinNameField">Наименование поля с ИНН организации.</param>
    /// <param name="trrcNameField">Наименование поля с КПП организации.</param>
    /// <returns>Вероятность.</returns>
    [Public]
    public virtual double GetTinTrrcFieldsProbability(IArioFact fact,
                                                      string tinNameField,
                                                      string trrcNameField)
    {
      // Получить обобщенную вероятность по полям ИНН и КПП.
      // Вес вероятности ИНН - 0.65, вес вероятности КПП - 0.35.
      var weightedFields = new Dictionary<IArioFactField, double>();
      var tinField = Commons.PublicFunctions.Module.GetField(fact, tinNameField);
      if (tinField != null)
        weightedFields.Add(tinField, 0.65);
      var trrcField = Commons.PublicFunctions.Module.GetField(fact, trrcNameField);
      if (trrcField != null)
        weightedFields.Add(trrcField, 0.35);
      
      return Commons.PublicFunctions.Module.GetAggregateFieldsProbability(weightedFields);
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям Наименование, ОПФ, ИНН, КПП.
    /// </summary>
    /// <param name="tinTrrcFact">Факт с ИНН, КПП организации.</param>
    /// <param name="nameLegalFormFact">Факт с наименованием, ОПФ организации.</param>
    /// <param name="tinNameField">Наименование поля с ИНН организации.</param>
    /// <param name="trrcNameField">Наименование поля с КПП организации.</param>
    /// <param name="counterpartyNameField">Наименование поля с наименованием организации.</param>
    /// <param name="counterpartyLegalFormField">Наименование поля с ОПФ организации.</param>
    /// <returns>Вероятность.</returns>
    [Public]
    public virtual double GetNameTinTrrcFieldsProbability(IArioFact tinTrrcFact,
                                                          IArioFact nameLegalFormFact,
                                                          string tinNameField,
                                                          string trrcNameField,
                                                          string counterpartyNameField,
                                                          string counterpartyLegalFormField)
    {
      // Получить обобщенную вероятность по полям ИНН, КПП, Наименование, ОПФ.
      // Вес вероятности Наименования - 0.4,
      // вес вероятности ОПФ - 0.1,
      // вес вероятности ИНН - 0.4,
      // вес вероятности КПП - 0.1.
      var weightedFields = new Dictionary<IArioFactField, double>();
      var tinField = Commons.PublicFunctions.Module.GetField(tinTrrcFact, tinNameField);
      if (tinField != null)
        weightedFields.Add(tinField, 0.4);
      var trrcField = Commons.PublicFunctions.Module.GetField(tinTrrcFact, trrcNameField);
      if (trrcField != null)
        weightedFields.Add(trrcField, 0.1);
      var nameField = Commons.PublicFunctions.Module.GetField(nameLegalFormFact, counterpartyNameField);
      if (nameField != null)
        weightedFields.Add(nameField, 0.4);
      var legalFormField = Commons.PublicFunctions.Module.GetField(nameLegalFormFact, counterpartyLegalFormField);
      if (legalFormField != null)
        weightedFields.Add(legalFormField, 0.1);
      
      return Commons.PublicFunctions.Module.GetAggregateFieldsProbability(weightedFields);
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания подписанта нашей стороны по факту, идентичному переданному.
    /// </summary>
    /// <param name="fact">Факт Арио.</param>
    /// <param name="propertyName">Имя свойства, связанного с подписантом.</param>
    /// <param name="businessUnit">НОР для фильтрации сотрудников.</param>
    /// <param name="businessUnitPropertyName">Имя связанного свойства НОР.</param>
    /// <returns>Структура, содержащая сотрудника, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetPreviousOurSignatoryRecognitionResults(IArioFact fact,
                                                                                 string propertyName,
                                                                                 IBusinessUnit businessUnit,
                                                                                 string businessUnitPropertyName)
    {
      var result = RecognizedOfficial.Create(null, null, fact,
                                             Module.PropertyProbabilityLevels.Min);
      
      var employeeRecognitionInfo = businessUnit != null
        ? Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName, businessUnit.Id.ToString(),
                                                                               businessUnitPropertyName)
        : Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName);
      
      if (employeeRecognitionInfo == null)
        return result;
      
      int employeeId;
      if (!int.TryParse(employeeRecognitionInfo.VerifiedValue, out employeeId))
        return result;
      
      var employee = Employees.GetAll(x => x.Id == employeeId)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active).FirstOrDefault();
      
      if (employee != null)
      {
        result.Employee = employee;
        result.Probability = employeeRecognitionInfo.Probability;
      }
      return result;
    }

    /// <summary>
    /// Получить контактное лицо по данным из факта и контрагента.
    /// </summary>
    /// <param name="contactFact">Факт, содержащий сведения о контакте.</param>
    /// <param name="propertyName">Имя связанного свойства.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyPropertyName">Имя связанного свойства контрагента.</param>
    /// <param name="recognizedContactNaming">Полное и краткое ФИО персоны.</param>
    /// <returns>Структура, содержащая контактное лицо, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetRecognizedContact(IArioFact contactFact,
                                                            string propertyName,
                                                            ICounterparty counterparty,
                                                            string counterpartyPropertyName,
                                                            IRecognizedPersonNaming recognizedContactNaming)
    {
      var signedBy = RecognizedOfficial.Create(null, Sungero.Parties.Contacts.Null, contactFact, Module.PropertyProbabilityLevels.Min);
      
      if (contactFact == null)
        return signedBy;

      // Если для свойства propertyName по факту существует верифицированное ранее значение, то вернуть его.
      signedBy = this.GetPreviousContactRecognitionResults(contactFact, propertyName, counterparty, counterpartyPropertyName);
      if (signedBy.Contact != null)
        return signedBy;
      
      var contacts = new List<IContact>().AsQueryable();
      
      var fullName = recognizedContactNaming.FullName;
      var shortName = recognizedContactNaming.ShortName;
      
      if (!string.IsNullOrWhiteSpace(fullName) || !string.IsNullOrWhiteSpace(shortName))
      {
        contacts = Parties.PublicFunctions.Contact.GetContactsByName(fullName, shortName, counterparty);
        
        if (!contacts.Any())
          contacts = Parties.PublicFunctions.Contact.GetContactsByName(shortName, shortName, counterparty);
      }
      
      contacts = contacts.Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      if (!contacts.Any())
        return signedBy;
      
      signedBy.Contact = contacts.FirstOrDefault();
      
      // Если нашли контакты по полному ФИО (Фамилия Имя Отчество), то вероятность максимальная. Иначе ниже среднего.
      // И вероятность зависит от количества найденных контактов.
      signedBy.Probability = string.Equals(signedBy.Contact.Name, fullName, StringComparison.InvariantCultureIgnoreCase) &&
        !string.Equals(fullName, shortName, StringComparison.InvariantCultureIgnoreCase) ?
        Module.PropertyProbabilityLevels.Max / contacts.Count() :
        Module.PropertyProbabilityLevels.LowerMiddle / contacts.Count();
      
      return signedBy;
    }
    
    /// <summary>
    /// Получить полное и краткое ФИО персоны из факта.
    /// </summary>
    /// <param name="personFact">Факт, содержащий сведения о персоне.</param>
    /// <param name="surnameField">Наименование поля с фамилией персоны.</param>
    /// <param name="nameField">Наименование поля с именем персоны.</param>
    /// <param name="patronymicField">Наименование поля с отчеством персоны.</param>
    /// <returns>Полное и краткое ФИО персоны.</returns>
    [Public]
    public virtual IRecognizedPersonNaming GetRecognizedPersonNaming(IArioFact personFact,
                                                                     string surnameField, string nameField,
                                                                     string patronymicField)
    {
      var recognizedPersonNaming = RecognizedPersonNaming.Create();
      
      if (personFact == null)
        return recognizedPersonNaming;
      
      var surname = Commons.PublicFunctions.Module.GetFieldValue(personFact, surnameField);
      var name = Commons.PublicFunctions.Module.GetFieldValue(personFact, nameField);
      var patronymic = Commons.PublicFunctions.Module.GetFieldValue(personFact, patronymicField);
      
      // Собрать полные ФИО из фамилии, имени и отчества.
      var parts = new List<string>();
      
      if (!string.IsNullOrWhiteSpace(surname))
        parts.Add(surname);
      if (!string.IsNullOrWhiteSpace(name))
        parts.Add(name);
      if (!string.IsNullOrWhiteSpace(patronymic))
        parts.Add(patronymic);
      
      recognizedPersonNaming.FullName = string.Join(" ", parts);
      
      // Собрать краткое ФИО.
      var shortName = string.Empty;
      
      // Если 2 из 3 полей пустые, то скорее всего сервис Ario вернул Фамилию И.О. в третье поле.
      if (string.IsNullOrWhiteSpace(surname) && string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(patronymic))
        shortName = patronymic;
      
      if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(patronymic) && !string.IsNullOrWhiteSpace(surname))
        shortName = surname;
      
      if (string.IsNullOrWhiteSpace(surname) && string.IsNullOrWhiteSpace(patronymic) && !string.IsNullOrWhiteSpace(name))
        shortName = name;
      
      if (string.IsNullOrEmpty(shortName) && (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(surname) ||
                                              !string.IsNullOrWhiteSpace(patronymic)))
        shortName = Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(name, patronymic, surname);
      
      if (!string.IsNullOrEmpty(shortName))
      {
        var nonBreakingSpace = new string('\u00A0', 1);
        var space = new string('\u0020', 1);
        
        // Короткое имя персоны содержит неразрывный пробел.
        shortName = shortName.Replace(". ", ".").Replace(space, nonBreakingSpace);
      }
      
      recognizedPersonNaming.ShortName = shortName;
      
      return recognizedPersonNaming;
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания контактного лица контрагента по факту, идентичному переданному,
    /// с фильтрацией по контрагенту.
    /// </summary>
    /// <param name="fact">Факт Арио.</param>
    /// <param name="propertyName">Имя свойства, связанного с контактом.</param>
    /// <param name="counterparty">Контрагент для дополнительной фильтрации контактных лиц.</param>
    /// <param name="counterpartyPropertyName">Имя связанного свойства контрагента.</param>
    /// <returns>Структура, содержащая контактное лицо, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetPreviousContactRecognitionResults(IArioFact fact,
                                                                            string propertyName,
                                                                            Parties.ICounterparty counterparty,
                                                                            string counterpartyPropertyName)
    {
      var result = RecognizedOfficial.Create(null, Parties.Contacts.Null, fact,
                                             Module.PropertyProbabilityLevels.Min);
      
      var contactRecognitionInfo = counterparty != null
        ? Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName, counterparty.Id.ToString(),
                                                                               counterpartyPropertyName)
        : Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName);
      
      if (contactRecognitionInfo == null)
        return result;
      
      int contactId;
      if (!int.TryParse(contactRecognitionInfo.VerifiedValue, out contactId))
        return result;
      
      var contact = Parties.Contacts.GetAll(x => x.Id == contactId)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active).FirstOrDefault();
      if (contact != null)
      {
        result.Contact = contact;
        result.Probability = contactRecognitionInfo.Probability;
      }
      return result;
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания контрагента по факту, идентичному переданному.
    /// </summary>
    /// <param name="fact">Факт Арио.</param>
    /// <param name="propertyName">Имя свойства, связанного контрагентом.</param>
    /// <returns>Структура, содержащая контрагента, подтвержденного пользователем, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedCounterparty GetPreviousCounterpartyRecognitionResults(IArioFact fact,
                                                                                     string propertyName)
    {
      var counterpartyRecognitionInfo = Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName);
      if (counterpartyRecognitionInfo == null)
        return null;
      
      int counterpartyId;
      if (!int.TryParse(counterpartyRecognitionInfo.VerifiedValue, out counterpartyId))
        return null;
      
      var counterparty = Parties.Counterparties.GetAll(x => x.Id == counterpartyId).FirstOrDefault();
      if (counterparty == null)
        return null;
      
      var result = RecognizedCounterparty.Create();
      result.Counterparty = counterparty;
      result.Fact = fact;
      result.CounterpartyProbability = counterpartyRecognitionInfo.Probability.HasValue ?
        counterpartyRecognitionInfo.Probability.Value :
        Module.PropertyProbabilityLevels.Min;
      return result;
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания НОР по факту, идентичному переданному.
    /// </summary>
    /// <param name="fact">Факт Арио.</param>
    /// <param name="propertyName">Имя свойства, связанного с НОР.</param>
    /// <returns>Структура с НОР, подтвержденной пользователем, фактом и вероятностью.</returns>
    [Public]
    public virtual IRecognizedCounterparty GetPreviousBusinessUnitRecognitionResults(IArioFact fact,
                                                                                     string propertyName)
    {
      var businessUnitRecognitionInfo = Commons.PublicFunctions.Module.GetPreviousPropertyRecognitionResults(fact, propertyName);
      if (businessUnitRecognitionInfo == null)
        return null;
      
      int businessUnitId;
      if (!int.TryParse(businessUnitRecognitionInfo.VerifiedValue, out businessUnitId))
        return null;
      
      var businessUnit = BusinessUnits.GetAll(x => x.Id == businessUnitId).FirstOrDefault();
      if (businessUnit == null)
        return null;
      
      var result = RecognizedCounterparty.Create();
      result.BusinessUnit = businessUnit;
      result.Fact = fact;
      result.BusinessUnitProbability = businessUnitRecognitionInfo.Probability;
      return result;
    }
    
    /// <summary>
    /// Получить наименование контрагента.
    /// </summary>
    /// <param name="fact">Исходный факт, содержащий наименование контрагента.</param>
    /// <param name="nameFieldName">Наименование поля с наименованием контрагента.</param>
    /// <param name="legalFormFieldName">Наименование поля с организационно-правовой формой контрагента.</param>
    /// <returns>Наименование контрагента.</returns>
    [Public]
    public static string GetCounterpartyName(IArioFact fact, string nameFieldName, string legalFormFieldName)
    {
      if (fact == null)
        return string.Empty;
      
      var name = Commons.PublicFunctions.Module.GetFieldValue(fact, nameFieldName);
      var legalForm = Commons.PublicFunctions.Module.GetFieldValue(fact, legalFormFieldName);
      return string.IsNullOrEmpty(legalForm) ? name : string.Format("{0}, {1}", name, legalForm);
    }
    
    #endregion
    
    #region Получение адресатов
    
    /// <summary>
    /// Получить адресатов по имени.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <returns>Список адресатов.</returns>
    [Public]
    public virtual List<IEmployee> GetAddresseesByName(string name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return null;
      
      var oldChar = "ё";
      var newChar = "е";
      name = name.ToLower().Replace(oldChar, newChar);
      
      var employees = new List<IEmployee>();
      var activeEmployees = Company.Employees.GetAll(e => e.Status == Company.Employee.Status.Active);
      // Прямое совпадение по 'name'.
      employees = activeEmployees.Where(e => e.Name.ToLower().Replace(oldChar, newChar) == name).ToList();
      if (employees.Any())
        return employees;
      
      // Поиск только по фамилии персоны, если передана только фамилия - старое поведение.
      employees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == name).ToList();
      if (employees.Any())
        return employees;
      
      // Поискать без преобразования в именительный падеж.
      var nameVariants = this.GetNameVariants(name);
      foreach (var nameVariant in nameVariants)
        employees.AddRange(this.GetEmployeesByName(nameVariant).Where(x => x != null));
      if (employees.Any())
        return employees;
      
      // Поискать с предварительным преобразованием в именительный падеж с мужским фейковым отчеством.
      employees = this.GetEmployeesByDativeDeclensionName(nameVariants, CommonLibrary.Gender.Masculine);
      if (employees.Any())
        return employees;
      
      // Поискать с предварительным преобразованием в именительный падеж с женским фейковым отчеством.
      employees = this.GetEmployeesByDativeDeclensionName(nameVariants, CommonLibrary.Gender.Feminine);
      return employees;
    }
    
    /// <summary>
    /// Получить возможные варианты ФИО из строки.
    /// </summary>
    /// <param name="name">Строка с ФИО.</param>
    /// <returns>Персональные данные.</returns>
    /// <remarks>Реализован парсинг следующих форматов ФИО:
    /// <para>- Иванов  Иван;</para>
    /// <para>- Иванов  Иван  Иванович;</para>
    /// <para>- Иван  Иванов;</para>
    /// <para>- Иван  Иванович  Иванов;</para>
    /// <para>- Иванов  И.  И.;</para>
    /// <para>- Иванов  И.;</para>
    /// <para>- Иванов  И.И.;</para>
    /// <para>- Иванов  И  И;</para>
    /// <para>- Иванов  И;</para>
    /// <para>- Иванов  ИИ;</para>
    /// <para>- И. И. Иванов;</para>
    /// <para>- И. Иванов;</para>
    /// <para>- И.И. Иванов;</para>
    /// <para>- И И  Иванов;</para>
    /// <para>- И Иванов;</para>
    /// <para>- ИИ Иванов.</para></remarks>
    [Public]
    public virtual List<CommonLibrary.PersonFullName> GetNameVariants(string name)
    {
      var variants = new List<CommonLibrary.PersonFullName>();
      
      if (string.IsNullOrWhiteSpace(name))
        return variants;
      
      variants.AddRange(this.GetLongNameVariants(name));
      variants.AddRange(this.GetShortNameVariants(name));
      
      return variants;
    }
    
    /// <summary>
    /// Получить возможные варианты полных ФИО из строки.
    /// </summary>
    /// <param name="name">Строка с ФИО.</param>
    /// <returns>Персональные данные.</returns>
    /// <remarks>ФИО может быть без отчества.</remarks>
    [Public]
    public virtual List<CommonLibrary.PersonFullName> GetLongNameVariants(string name)
    {
      var variants = new List<CommonLibrary.PersonFullName>();
      if (string.IsNullOrWhiteSpace(name))
        return variants;
      
      var nameMatching = System.Text.RegularExpressions.Regex.Match(name, @"^(\S+)(?<!\.)\s*(\S+)(?<!\.)\s*(\S*)(?<!\.)$");
      if (!nameMatching.Success)
        return variants;
      
      /* Иванов Иван Иванович
       * Иванов Иван
       * PersonFullName ctor params: LastName, FirstName, MiddleName.
       */
      variants.Add(CommonLibrary.PersonFullName.Create(nameMatching.Groups[1].Value, nameMatching.Groups[2].Value, nameMatching.Groups[3].Value));
      
      /* Иван Иванович Иванов
       * Иван Иванов
       * PersonFullName ctor params: LastName, FirstName, MiddleName.
       */
      variants.Add(CommonLibrary.PersonFullName.Create(nameMatching.Groups[3].Value, nameMatching.Groups[1].Value, nameMatching.Groups[2].Value));
      
      return variants;
    }
    
    /// <summary>
    /// Получить возможные варианты коротких ФИО из строки.
    /// </summary>
    /// <param name="name">Строка с ФИО.</param>
    /// <returns>Персональные данные.</returns>
    /// <remarks>ФИО может быть без отчества.</remarks>
    [Public]
    public virtual List<CommonLibrary.PersonFullName> GetShortNameVariants(string name)
    {
      var variants = new List<CommonLibrary.PersonFullName>();
      if (string.IsNullOrWhiteSpace(name))
        return variants;
      
      /* Иванов  И.  И.
       * Иванов  И.
       * Иванов  И.И.
       * Иванов  И  И
       * Иванов  И
       * Иванов  ИИ
       * PersonFullName ctor params: LastName, FirstName, MiddleName.
       */
      var nameMatching = System.Text.RegularExpressions.Regex.Match(name, @"^(\S+)\s*(\S)\.?\s*(\S?)(?<!\.)\.?$");
      if (nameMatching.Success)
        variants.Add(CommonLibrary.PersonFullName.Create(nameMatching.Groups[1].Value, nameMatching.Groups[2].Value, nameMatching.Groups[3].Value));
      
      /* И. И. Иванов
       * И. Иванов
       * И.И. Иванов
       * И И  Иванов
       * И Иванов
       * ИИ Иванов
       * PersonFullName ctor params: LastName, FirstName, MiddleName.
       */
      nameMatching = System.Text.RegularExpressions.Regex.Match(name, @"^(\S)\.?\s*(\S?)(?<!\.)\.?\s+(\S+)$");
      if (nameMatching.Success)
        variants.Add(CommonLibrary.PersonFullName.Create(nameMatching.Groups[3].Value, nameMatching.Groups[1].Value, nameMatching.Groups[2].Value));
      
      return variants;
    }
    
    /// <summary>
    /// Получить сотрудников по ФИО.
    /// </summary>
    /// <param name="name">Персональные данные.</param>
    /// <returns>Список сотрудников.</returns>
    [Public]
    public virtual List<IEmployee> GetEmployeesByName(CommonLibrary.PersonFullName name)
    {
      if (name == null)
        return new List<IEmployee>();
      
      var oldChar = "ё";
      var newChar = "е";
      var activeEmployees = Company.Employees.GetAll(e => e.Status == Company.Employee.Status.Active);
      var employees = activeEmployees.Where(x => x.Person.LastName.ToLower().Replace(oldChar, newChar) == name.LastName.ToLower()).ToList();
      
      // Уточнить по имени.
      if (!string.IsNullOrWhiteSpace(name.FirstName))
      {
        employees = name.FirstName.Length != 1
          ? employees.Where(x => x.Person.FirstName.ToLower().Replace(oldChar, newChar) == name.FirstName.ToLower()).ToList()
          : employees.Where(x => x.Person.FirstName.ToLower().Replace(oldChar, newChar)[0] == name.FirstName.ToLower()[0]).ToList();
      }
      
      // Уточнить по отчеству.
      if (!string.IsNullOrWhiteSpace(name.MiddleName))
      {
        employees = name.MiddleName.Length != 1
          ? employees.Where(x => x.Person.MiddleName.ToLower().Replace(oldChar, newChar) == name.MiddleName.ToLower()).ToList()
          : employees.Where(x => x.Person.MiddleName.ToLower().Replace(oldChar, newChar)[0] == name.MiddleName.ToLower()[0]).ToList();
      }
      
      return employees;
    }
    
    /// <summary>
    /// Получить сотрудников по ФИО в дательном падеже.
    /// </summary>
    /// <param name="nameVariants">Персональные данные.</param>
    /// <param name="gender">Пол.</param>
    /// <returns>Список сотрудников.</returns>
    [Public]
    public virtual List<IEmployee> GetEmployeesByDativeDeclensionName(List<CommonLibrary.PersonFullName> nameVariants, CommonLibrary.Gender gender)
    {
      var employees = new List<IEmployee>();
      foreach (var nameVariant in nameVariants)
      {
        var fullNameInDeclension = this.FromDativeToNominativeDeclension(nameVariant, gender);
        if (fullNameInDeclension != null)
          employees.AddRange(this.GetEmployeesByName(fullNameInDeclension).Where(x => x != null));
      }
      return employees;
    }
    
    /// <summary>
    /// Преобразовать ФИО из дательного падежа в именительный.
    /// </summary>
    /// <param name="name">Персональные данные.</param>
    /// <param name="gender">Пол.</param>
    /// <returns>ФИО в именительном падеже.</returns>
    /// <remarks>Для корректного преобразования из дательного падежа в именительный используется фейковое отчество.
    /// После преобразования к именительному падежу восстанавливается исходное отчество.</remarks>
    [Public]
    public virtual PersonFullName FromDativeToNominativeDeclension(PersonFullName name, CommonLibrary.Gender gender)
    {
      var maleTempMiddleName = "Александровичу";
      var femaleTempMiddleName = "Александровне";
      var replaceOriginalMiddleName =
        gender != CommonLibrary.Gender.NotDefined &&
        (string.IsNullOrWhiteSpace(name.MiddleName) || name.MiddleName.Length == 1);
      var nameInDeclensionStr = string.Empty;
      if (replaceOriginalMiddleName)
      {
        nameInDeclensionStr = gender == CommonLibrary.Gender.Masculine
          ? Padeg.ToNominativeDeclension(PersonFullName.Create(name.LastName, name.FirstName, maleTempMiddleName))
          : Padeg.ToNominativeDeclension(PersonFullName.Create(name.LastName, name.FirstName, femaleTempMiddleName));
      }
      else
      {
        nameInDeclensionStr = Padeg.ToNominativeDeclension(name);
      }
      
      PersonFullName nameInDeclension;
      if (PersonFullName.TryParse(nameInDeclensionStr, out nameInDeclension) && replaceOriginalMiddleName)
        nameInDeclension = PersonFullName.Create(nameInDeclension.LastName, nameInDeclension.FirstName, name.MiddleName);
      
      return nameInDeclension;
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение свойств документов с нечетким поиском
    
    #region Входящее письмо
    
    /// <summary>
    /// Заполнить свойства входящего письма по результатам обработки Ario. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillIncomingLetterPropertiesFuzzy(RecordManagement.IIncomingLetter letter,
                                                          IDocumentInfo documentInfo,
                                                          Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      var props = letter.Info.Properties;
      
      // Вид документа.
      this.FillDocumentKind(letter);
      
      // Содержание.
      this.FillIncomingLetterSubject(letter, arioDocument);
      
      // Номер и дата.
      this.FillDocumentNumberAndDateFuzzy(letter, arioDocument, ArioGrammars.LetterFact.Name,
                                          props.InNumber, ArioGrammars.LetterFact.NumberField,
                                          props.Dated, ArioGrammars.LetterFact.DateField);

      // НОР, адресат, подразделение.
      this.FillIncomingLetterRecipientFuzzy(letter, arioDocument, responsible);

      // Корреспондент.
      this.FillIncomingLetterCorrespondentFuzzy(letter, arioDocument);

      // Контактные лица (подписант, исполнитель).
      this.FillIncomingLetterContactsFuzzy(letter, arioDocument);
      
      // Реквизиты искодящего письма ("в ответ на").
      this.FillIncomingLetterResponseToFuzzy(letter, arioDocument);
    }
    
    /// <summary>
    /// Заполнить корреспондента входящего письма.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    /// <remarks>С использованием нечеткого поиска.</remarks>
    [Public]
    public virtual void FillIncomingLetterCorrespondentFuzzy(IIncomingLetter letter, IArioDocument arioDocument)
    {
      // Задать приоритет полей для поиска корреспондента. Вес определяет как учитывается вероятность каждого поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.LetterFact.TinField, 0.35 },
        { ArioGrammars.LetterFact.TrrcField, 0.05 },
        { ArioGrammars.LetterFact.PsrnField, 0.25 },
        { ArioGrammars.LetterFact.CorrespondentNameField, 0.15 },
        { ArioGrammars.LetterFact.HeadCompanyNameField, 0.05 },
        { ArioGrammars.LetterFact.WebsiteField, 0.05 },
        { ArioGrammars.LetterFact.PhoneField, 0.05 },
        { ArioGrammars.LetterFact.EmailField, 0.05 }
      };

      // Подобрать факт Letter с полем типа Correspondent. Игнорировать факты Counterparty, содержащие данные о прочих организациях.
      var counterpartyFacts = arioDocument.Facts
        .Where(f => f.Name == ArioGrammars.LetterFact.Name &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.LetterFact.TypeField &&
                            fl.Value == ArioGrammars.LetterFact.CorrespondentTypes.Correspondent) &&
               f.Fields.Any(fl => weightedFields.ContainsKey(fl.Name) && !string.IsNullOrEmpty(fl.Value)))
        .ToList();
      
      if (!counterpartyFacts.Any())
        return;

      // Отсортировать факты по количеству и приоритету полей.
      var orderedCounterpartyFacts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(counterpartyFacts, ArioGrammars.LetterFact.Name, weightedFields);
      var propertyName = letter.Info.Properties.Correspondent.Name;

      foreach (var fact in orderedCounterpartyFacts)
      {
        // Получить корреспондента по значениям полей факта с использованием Elasticsearch.
        var searchResult = this.SearchLetterCorrespondentFuzzy(fact);

        if (searchResult.EntityId == 0)
          continue;
        
        // Если корреспондент найден, рассчитать вероятность факта. Заполнить свойство в карточке документа и связь в результатах распознавания.
        var counterparty = Parties.Counterparties.GetAll(x => Equals(x.Id, searchResult.EntityId)).FirstOrDefault();
        if (counterparty == null)
          continue;
        
        letter.Correspondent = counterparty;
        
        var recognizedFields = new Dictionary<IArioFactField, double>();
        foreach (var field in searchResult.FoundedFields)
          recognizedFields.Add(field, weightedFields[field.Name]);
        
        var factProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                fact,
                                                                                searchResult.FoundedFields.Select(fl => fl.Name).ToList(),
                                                                                propertyName,
                                                                                counterparty,
                                                                                factProbability,
                                                                                null);
        return;
      }
    }
    
    /// <summary>
    /// Заполнить нашу организацию, адресата и подразделение. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    /// <param name="responsible">Ответственный за обработку документов.</param>
    [Public]
    public virtual void FillIncomingLetterRecipientFuzzy(IIncomingLetter letter, IArioDocument arioDocument, IEmployee responsible)
    {
      var allBusinessUnits = Sungero.Company.BusinessUnits.GetAll();
      if (!allBusinessUnits.Any())
        return;

      var recognizedBusinessUnit = RecognizedCounterparty.Create();
      var recognizedAddressees = new List<IRecognizedOfficial>();

      // Получить список распознанных НОР и адресатов из фактов.
      var recognizedRecipients = this.GetRecognizedRecipientsFuzzy(arioDocument.Facts);
      
      // Получить НОР из карточки ответственного за обработку. Считать ее приоритетной при выборе из нескольких НОР.
      var defaultbusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);
      if (defaultbusinessUnit == null && allBusinessUnits.Count() == 1)
        defaultbusinessUnit = allBusinessUnits.Single();

      if (recognizedRecipients.Any())
      {
        // Если распознано несколько НОР, приоритетнее организация с адресатом.
        var recognizedRecipient = recognizedRecipients.First();
        if (defaultbusinessUnit != null && recognizedRecipients.Any(x => Equals(x.BusinessUnit.BusinessUnit, defaultbusinessUnit)))
          recognizedRecipient = recognizedRecipients.First(x => Equals(x.BusinessUnit.BusinessUnit, defaultbusinessUnit));
        else if (recognizedRecipients.Any(x => x.Addressees.Any()))
          recognizedRecipient = recognizedRecipients.First(x => x.Addressees.Any());
        
        recognizedBusinessUnit = recognizedRecipient.BusinessUnit;
        recognizedAddressees = recognizedRecipient.Addressees;
      }
      else if (defaultbusinessUnit != null)
      {
        // Если НОР не найдена по фактам. Взять НОР, полученную по ответственному за обработку.
        recognizedBusinessUnit.Fact = null;
        recognizedBusinessUnit.BusinessUnit = defaultbusinessUnit;
        recognizedBusinessUnit.BusinessUnitProbability = Module.PropertyProbabilityLevels.Min;
      }

      // Заполнить карточку.
      if (recognizedBusinessUnit.BusinessUnit != null)
      {
        letter.BusinessUnit = recognizedBusinessUnit.BusinessUnit;
        var fieldNames = recognizedBusinessUnit.Fact != null ? recognizedBusinessUnit.Fact.Fields.Select(x => x.Name).ToList() : null;
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                recognizedBusinessUnit.Fact,
                                                                                fieldNames,
                                                                                letter.Info.Properties.BusinessUnit.Name,
                                                                                recognizedBusinessUnit.BusinessUnit,
                                                                                recognizedBusinessUnit.BusinessUnitProbability,
                                                                                null);
      }
      
      if (recognizedAddressees.Count == 1)
      {
        var recognizedAddressee = recognizedAddressees.Single();
        letter.Addressee = recognizedAddressee.Employee;

        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                                  recognizedAddressee.Fact,
                                                                                  ArioGrammars.LetterFact.AddresseeField,
                                                                                  letter.Info.Properties.Addressee.Name,
                                                                                  letter.Addressee,
                                                                                  recognizedAddressee.Probability);
      }
      else if (recognizedAddressees.Count > 1)
      {
        letter.IsManyAddressees = true;
        foreach (var addressee in recognizedAddressees)
        {
          var row = letter.Addressees.AddNew();
          row.Addressee = addressee.Employee;
          Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                          addressee.Fact,
                                                                                          new List<string> { ArioGrammars.LetterFact.AddresseeField },
                                                                                          row.Info.Properties.Addressee.Name,
                                                                                          row.Addressee,
                                                                                          addressee.Probability,
                                                                                          row.Id);
        }
      }
      
      // Заполнить подразделение.
      letter.Department = letter.Addressee != null
        ? Company.PublicFunctions.Department.GetDepartment(letter.Addressee)
        : Company.PublicFunctions.Department.GetDepartment(responsible);
    }

    /// <summary>
    /// Заполнить подписанта и исполнителя входящего письма.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    /// <remarks>С использованием нечеткого поиска.</remarks>
    [Public]
    public virtual void FillIncomingLetterContactsFuzzy(IIncomingLetter letter, IArioDocument arioDocument)
    {
      var filterCounterpartyId = letter.Correspondent != null ? letter.Correspondent.Id : 0;
      var probability = 0d;
      
      // Подписант.
      var signatory = this.GetRecognizedContactFuzzy(arioDocument, ArioGrammars.LetterPersonFact.PersonTypes.Signatory, filterCounterpartyId);
      
      if (signatory.Contact != null)
      {
        letter.SignedBy = signatory.Contact;
        probability += signatory.Probability.GetValueOrDefault();
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo, signatory.Fact,
                                                                          null, letter.Info.Properties.SignedBy.Name,
                                                                          letter.SignedBy,
                                                                          signatory.Probability);
        
        // Если удалось найти подписанта. Искать исполнителя в той же компании.
        if (filterCounterpartyId == 0)
          filterCounterpartyId = letter.SignedBy.Company.Id;
      }
      
      // Исполнитель.
      var responsible = this.GetRecognizedContactFuzzy(arioDocument, ArioGrammars.LetterPersonFact.PersonTypes.Responsible, filterCounterpartyId);
      if (responsible.Contact != null)
      {
        letter.Contact = responsible.Contact;
        probability += responsible.Probability.GetValueOrDefault();
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo, responsible.Fact,
                                                                          null, letter.Info.Properties.Contact.Name,
                                                                          letter.Contact,
                                                                          responsible.Probability);
      }
      
      // Установить корреспондента по контактам.
      if (letter.Correspondent == null && (signatory.Contact != null || responsible.Contact != null))
      {
        var recognizedCorrespondent = signatory.Contact != null ? signatory.Contact.Company : responsible.Contact.Company;
        letter.Correspondent = recognizedCorrespondent;
        
        // Вероятность для подсветки определить по средней вероятности подписанта и исполнителя.
        var recognizedCorrespondentProbability = Module.PropertyProbabilityLevels.Min;
        if (recognizedCorrespondent.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
            (probability / 2) >= Module.PropertyProbabilityLevels.UpperMiddle)
          recognizedCorrespondentProbability = Module.PropertyProbabilityLevels.UpperMiddle;

        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo, null, null,
                                                                          letter.Info.Properties.Correspondent.Name,
                                                                          letter.Correspondent,
                                                                          recognizedCorrespondentProbability);
      }
    }

    /// <summary>
    /// Заполнить реквизиты исходящего письма (свойство "в ответ на").
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    [Public]
    public virtual void FillIncomingLetterResponseToFuzzy(IIncomingLetter letter, IArioDocument arioDocument)
    {
      if (letter.BusinessUnit == null)
        return;

      // Получить факты с номером и/или датой исходящего документа. Приоритетнее факты, содержащие оба поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.LetterFact.ResponseToNumberField, 0.5 },
        { ArioGrammars.LetterFact.ResponseToDateField, 0.5 }
      };
      var facts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(arioDocument.Facts, ArioGrammars.LetterFact.Name, weightedFields);

      if (!facts.Any())
        return;
      
      // Подготовить список исходящих писем для поиска.
      var outgoingLetters = RecordManagement.OutgoingLetters.GetAll(d => Equals(d.BusinessUnit, letter.BusinessUnit));
      if (letter.Correspondent != null)
        outgoingLetters = outgoingLetters.Where(d => Equals(d.Correspondent, letter.Correspondent));
      
      // Найти документ по номеру и дате.
      var recognizedDocument = this.GetRecognizedDocumentFuzzy(facts,
                                                               outgoingLetters,
                                                               ArioGrammars.LetterFact.ResponseToNumberField,
                                                               ArioGrammars.LetterFact.ResponseToDateField);
      if (recognizedDocument.Document != null)
      {
        letter.InResponseTo = OutgoingDocumentBases.As(recognizedDocument.Document);
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                        recognizedDocument.Fact,
                                                                                        weightedFields.Select(x => x.Key).ToList(),
                                                                                        letter.Info.Properties.InResponseTo.Name,
                                                                                        letter.InResponseTo,
                                                                                        recognizedDocument.Probability,
                                                                                        null);
      }
    }

    /// <summary>
    /// Получить список распознанных НОР и связанных с ними адресатов из фактов Ario.
    /// </summary>
    /// <param name="facts">Список фактов Ario.</param>
    /// <returns>Список структур с распознанными НОР и адресатами.</returns>
    [Public]
    public virtual List<IRecognizedRecipient> GetRecognizedRecipientsFuzzy(List<IArioFact> facts)
    {
      var recognizedRecipients = new List<IRecognizedRecipient>();
      if (!facts.Any())
        return recognizedRecipients;
      
      // Получить все распознанные НОР, связанные с фактами.
      var recognizedBusinessUnits = this.GetRecognizedLetterBusinessUnitsFuzzy(facts);

      // Получить адресатов.
      var recognizedAddressees = this.GetRecognizedLetterAddresseesFuzzy(facts, recognizedBusinessUnits);

      // Добавить НОР из адресатов в общий список распознанных.
      var addresseeBusinessUnits = recognizedAddressees
        .Where(x => x.Employee?.Department?.BusinessUnit != null)
        .Select(x => x.Employee.Department.BusinessUnit)
        .Distinct()
        .ToList();

      foreach (var businessUnit in addresseeBusinessUnits
               .Where(b => !recognizedBusinessUnits.Any(r => Equals(b, r.BusinessUnit))))
      {
        var recognizedBusinessUnit = RecognizedCounterparty.Create();
        recognizedBusinessUnit.BusinessUnit = businessUnit;
        recognizedBusinessUnit.BusinessUnitProbability = Constants.Module.PropertyProbabilityLevels.Min;
        recognizedBusinessUnits.Add(recognizedBusinessUnit);
      }

      // Для каждой НОР создать запись в результирующем списке.
      foreach (var recognizedBusinessUnit in recognizedBusinessUnits)
      {
        var businessUnit = recognizedBusinessUnit.BusinessUnit;
        if (recognizedRecipients.Any(x => Equals(x.BusinessUnit.BusinessUnit, businessUnit)))
          continue;
        
        var recognizedRecipient = RecognizedRecipient.Create(recognizedBusinessUnit, new List<IRecognizedOfficial>());
        
        // Собрать всех адресатов. Исключить дубли.
        var employees = recognizedAddressees
          .Where(x => Equals(x.Employee?.Department?.BusinessUnit, businessUnit))
          .Select(x => x.Employee)
          .Distinct()
          .ToList();
        
        foreach (var employee in employees)
        {
          // Если НОР определена по адресату, сбросить вероятность до минимальной.
          var recognizedAddressee = recognizedAddressees.First(x => Equals(x.Employee, employee));
          if (recognizedBusinessUnit.Fact == null)
            recognizedAddressee.Probability = Constants.Module.PropertyProbabilityLevels.Min;
          recognizedRecipient.Addressees.Add(recognizedAddressee);
        }
        
        recognizedRecipients.Add(recognizedRecipient);
      }
      return recognizedRecipients;
    }

    /// <summary>
    /// Получить список всех распознанных НОР во входящем письме. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="facts">Список фактов Ario.</param>
    /// <returns>Список распознанных НОР.</returns>
    public virtual List<IRecognizedCounterparty> GetRecognizedLetterBusinessUnitsFuzzy(List<IArioFact> facts)
    {
      var recognizedBusinessUnits = new List<IRecognizedCounterparty>();
      
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.LetterFact.TinField, 0.35 },
        { ArioGrammars.LetterFact.TrrcField, 0.05 },
        { ArioGrammars.LetterFact.PsrnField, 0.25 },
        { ArioGrammars.LetterFact.CorrespondentNameField, 0.20 },
        { ArioGrammars.LetterFact.AddresseeField, 0.15 }
      };
      
      var recipientFacts = facts
        .Where(f => f.Name == ArioGrammars.LetterFact.Name &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.LetterFact.TypeField &&
                            fl.Value == ArioGrammars.LetterFact.CorrespondentTypes.Recipient) &&
               f.Fields.Any(fl => weightedFields.ContainsKey(fl.Name) && !string.IsNullOrEmpty(fl.Value)))
        .ToList();
      
      if (!recipientFacts.Any())
        return recognizedBusinessUnits;
      
      var orderedFacts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(recipientFacts,
                                                                                         ArioGrammars.LetterFact.Name,
                                                                                         weightedFields);
      foreach (var fact in orderedFacts)
      {
        var searchBusinessUnitResult = this.SearchLetterBusinessUnitFuzzy(fact);
        if (searchBusinessUnitResult.EntityId > 0)
        {
          var businessUnit = Company.BusinessUnits.GetAll(x => Equals(x.Id, searchBusinessUnitResult.EntityId)).FirstOrDefault();
          if (businessUnit == null)
            continue;

          var recognizedBusinessUnit = RecognizedCounterparty.Create();
          recognizedBusinessUnit.BusinessUnit = businessUnit;
          // Ограничить список подсвечиваемых полей найденными значениями.
          // Чтобы не потерять поля в исходном факте, создать его копию.
          var factForLink = ArioFact.Create(fact.Id, fact.Name, searchBusinessUnitResult.FoundedFields);
          recognizedBusinessUnit.Fact = factForLink;
          recognizedBusinessUnit.Type = ArioGrammars.LetterFact.CorrespondentTypes.Recipient;
          // Рассчитать средневзвешенную вероятность факта по списку найденных полей.
          var recognizedFields = new Dictionary<IArioFactField, double>();
          foreach (var field in factForLink.Fields)
            recognizedFields.Add(field, weightedFields[field.Name]);
          recognizedBusinessUnit.BusinessUnitProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
          
          recognizedBusinessUnits.Add(recognizedBusinessUnit);
        }
      }
      return recognizedBusinessUnits;
    }

    /// <summary>
    /// Получить адресатов входящего письма по фактам Арио.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="recognizedBusinessUnits">Список распознанных НОР.</param>
    /// <returns>Список найденных адресатов.</returns>
    public virtual List<IRecognizedOfficial> GetRecognizedLetterAddresseesFuzzy(List<IArioFact> facts, List<IRecognizedCounterparty> recognizedBusinessUnits)
    {
      var recognizedAddressees = new List<IRecognizedOfficial>();
      
      var addresseeFacts = facts
        .Where(f => f.Name == ArioGrammars.LetterFact.Name &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.LetterFact.TypeField &&
                            fl.Value == ArioGrammars.LetterFact.CorrespondentTypes.Recipient) &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.LetterFact.AddresseeField && !string.IsNullOrEmpty(fl.Value)))
        .ToList();
      
      if (!addresseeFacts.Any())
        return recognizedAddressees;
      
      /* Искать сотрудников по всем фактам, содержащим ФИО адресата.
       * Фильтровать по конкретной НОР, если удалось ее определить по этому же факту.
       * Либо, в противном случае, искать ФИО адресата в каждой извлеченной НОР.
       * Если по одному ФИО найдено несколько сотрудников, выбрать из них руководителя НОР/подразделения.
       * Если в итоге осталось более одного однофамильца, считать поиск неточным и не возвращать никого.
       */

      var allBusinessUnitIds = recognizedBusinessUnits
        .Where(x => x.BusinessUnit != null)
        .Select(x => x.BusinessUnit.Id)
        .Distinct()
        .ToList();
      
      var managers = Company.BusinessUnits.GetAll(x => x.CEO != null).Select(x => x.CEO).ToList();
      managers.AddRange(Company.Departments.GetAll(x => x.Manager != null).Select(x => x.Manager));
      managers = managers.Distinct().ToList();

      foreach (var fact in addresseeFacts)
      {
        var factBusinessUnitIds = new List<int>();
        var recognizedBusinessUnit = recognizedBusinessUnits.FirstOrDefault(x => x.BusinessUnit != null && Equals(x.Fact?.Id, fact.Id));
        if (recognizedBusinessUnit != null)
          factBusinessUnitIds.Add(recognizedBusinessUnit.BusinessUnit.Id);
        else if (allBusinessUnitIds.Any())
          factBusinessUnitIds.AddRange(allBusinessUnitIds);
        else
          factBusinessUnitIds.Add(0);

        // В одном факте может быть несколько полей с адресатами. В этом случае обработать их все.
        foreach (var field in fact.Fields.Where(x => x.Name == ArioGrammars.LetterFact.AddresseeField))
        {
          foreach (var businessUnitId in factBusinessUnitIds)
          {
            var employees = Company.PublicFunctions.Employee.GetEmployeesByNameFuzzy(field.Value, businessUnitId);
            if (employees.Count > 1)
              employees = employees.Where(x => managers.Contains(x)).ToList();
            if (employees.Count == 1)
              recognizedAddressees.Add(RecognizedOfficial.Create(employees.First(), null, fact, field.Probability));
          }
        }
      }
      
      return recognizedAddressees;
    }
    
    #endregion

    #region Первичка

    /// <summary>
    /// Заполнить свойства акта выполненных работ по результатам обработки Ario. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="contractStatement">Акт выполненных работ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillContractStatementPropertiesFuzzy(FinancialArchive.IContractStatement contractStatement,
                                                             IDocumentInfo documentInfo,
                                                             Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(contractStatement);
      
      // Подразделение и ответственный.
      contractStatement.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      contractStatement.ResponsibleEmployee = responsible;
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(contractStatement, documentInfo);
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>()
      {
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller,
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer
      };
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterpartiesFuzzy(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault();
      var nonType = recognizedCounterparties.Where(m => m.Type == string.Empty).ToList();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, nonType, responsible);
      
      this.FillAccountingDocumentParties(contractStatement, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(contractStatement, documentInfo, ArioGrammars.DocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Договор.
      this.FillAccountingDocumentLeadingDocumentFuzzy(contractStatement, arioDocument);
    }

    /// <summary>
    /// Заполнить свойства накладной по результатам обработки Ario. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="waybill">Товарная накладная.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillWaybillPropertiesFuzzy(FinancialArchive.IWaybill waybill,
                                                   IDocumentInfo documentInfo,
                                                   Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(waybill);
      
      // Подразделение и ответственный.
      waybill.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      waybill.ResponsibleEmployee = responsible;
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(waybill, documentInfo);
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>();
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Supplier);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Payer);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      arioCounterpartyTypes.Add(ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterpartiesFuzzy(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Supplier).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Payer).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee).FirstOrDefault();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, responsible);
      
      this.FillAccountingDocumentParties(waybill, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(waybill, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Документ-основание.
      this.FillAccountingDocumentLeadingDocumentFuzzy(waybill, arioDocument);
    }

    /// <summary>
    /// Заполнить свойства универсального передаточного документа по результатам обработки Ario. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="universalTransferDocument">Универсальный передаточный документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillUniversalTransferDocumentPropertiesFuzzy(FinancialArchive.IUniversalTransferDocument universalTransferDocument,
                                                                     IDocumentInfo documentInfo,
                                                                     Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(universalTransferDocument);
      
      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(universalTransferDocument, documentInfo);
      
      // Подразделение и ответственный.
      universalTransferDocument.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      universalTransferDocument.ResponsibleEmployee = responsible;
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>()
      {
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller,
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer,
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper,
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee
      };

      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterpartiesFuzzy(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault() ??
        recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee).FirstOrDefault();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, responsible);
      
      this.FillAccountingDocumentParties(universalTransferDocument, documentInfo, recognizedDocumentParties);
      
      // Дата, номер и регистрация.
      this.FillDocumentRegistrationData(universalTransferDocument, documentInfo, ArioGrammars.FinancialDocumentFact.Name, Docflow.Resources.UnknownNumber);
      
      // Договор (документ-основание).
      this.FillAccountingDocumentLeadingDocumentFuzzy(universalTransferDocument, arioDocument);
      
      // Корректировочный документ.
      if (universalTransferDocument.IsAdjustment.HasValue && universalTransferDocument.IsAdjustment.Value == true)
        this.FillUniversalTransferDocumentCorrectedDocument(universalTransferDocument, documentInfo);
      else
        this.FillUniversalTransferDocumentRevisionInfo(universalTransferDocument, documentInfo);
    }

    /// <summary>
    /// Подобрать по факту контрагента и НОР.
    /// </summary>
    /// <param name="allFacts">Факты.</param>
    /// <param name="arioCounterpartyTypes">Типы фактов контрагентов.</param>
    /// <returns>Наши организации и контрагенты, найденные по фактам.</returns>
    [Public]
    public virtual List<IRecognizedCounterparty> GetRecognizedAccountingDocumentCounterpartiesFuzzy(List<IArioFact> allFacts, List<string> arioCounterpartyTypes)
    {
      var recognizedCounterparties = new List<IRecognizedCounterparty>();
      
      foreach (var counterpartyType in arioCounterpartyTypes)
      {
        var recognizedBusinessUnit = this.GetRecognizedBusinessUnitFuzzy(allFacts, counterpartyType);
        var recognizedCounterparty = this.GetRecognizedCounterpartyFuzzy(allFacts, counterpartyType);
        
        // Если по одному факту найдены и НОР, и КА. Объединить результаты в одной структуре. Если найдены по разным фактам, то НОР приоритетнее.
        if (recognizedBusinessUnit.BusinessUnit != null && recognizedCounterparty.Counterparty != null && Equals(recognizedBusinessUnit.Fact, recognizedCounterparty.Fact))
        {
          recognizedCounterparty.BusinessUnit = recognizedBusinessUnit.BusinessUnit;
          recognizedCounterparty.BusinessUnitProbability = recognizedBusinessUnit.BusinessUnitProbability;
          recognizedCounterparties.Add(recognizedCounterparty);
        }
        else if (recognizedBusinessUnit.BusinessUnit != null)
          recognizedCounterparties.Add(recognizedBusinessUnit);
        else if (recognizedCounterparty.Counterparty != null)
          recognizedCounterparties.Add(recognizedCounterparty);
      }
      
      return recognizedCounterparties;
    }

    #endregion

    #region Договорные документы, финансовые документы, счет на оплату

    /// <summary>
    /// Заполнить свойства во входящем счете на оплату. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="incomingInvoice">Входящий счет.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillIncomingInvoicePropertiesFuzzy(Contracts.IIncomingInvoice incomingInvoice,
                                                           IDocumentInfo documentInfo,
                                                           IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(incomingInvoice);
      
      // Подразделение.
      incomingInvoice.Department = Company.PublicFunctions.Department.GetDepartment(responsible);

      // Сумма и валюта.
      this.FillAccountingDocumentAmountAndCurrency(incomingInvoice, documentInfo);
      
      // НОР и КА.
      var arioCounterpartyTypes = new List<string>()
      {
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller,
        ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer
      };
      
      var recognizedCounterparties = this.GetRecognizedAccountingDocumentCounterpartiesFuzzy(arioDocument.Facts, arioCounterpartyTypes);
      var seller = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller).FirstOrDefault();
      var buyer = recognizedCounterparties.Where(m => m.Type == ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer).FirstOrDefault();
      var nonType = recognizedCounterparties.Where(m => m.Type == string.Empty).ToList();
      
      var recognizedDocumentParties = this.GetDocumentParties(buyer, seller, nonType, responsible);
      
      this.FillAccountingDocumentParties(incomingInvoice, documentInfo, recognizedDocumentParties);
      
      // Договор.
      this.FillIncomingInvoiceContractFuzzy(incomingInvoice, arioDocument);
      
      // Номер и дата.
      var props = incomingInvoice.Info.Properties;
      this.FillDocumentNumberAndDateFuzzy(incomingInvoice, arioDocument, ArioGrammars.FinancialDocumentFact.Name,
                                          props.Number, ArioGrammars.FinancialDocumentFact.NumberField,
                                          props.Date, ArioGrammars.FinancialDocumentFact.DateField);
    }

    /// <summary>
    /// Заполнить свойства договора по результатам обработки Ario.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillContractPropertiesFuzzy(Contracts.IContract contract,
                                                    IDocumentInfo documentInfo,
                                                    Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(contract);
      
      this.FillContractualDocumentAmountAndCurrency(contract, documentInfo);
      
      // Заполнить данные нашей стороны и корреспондента.
      this.FillContractualDocumentPartiesFuzzy(contract, documentInfo, responsible);
      
      // Заполнить ответственного после заполнения НОР и КА, чтобы вычислялась НОР из фактов, а не по отв.
      // Если так не сделать, то НОР заполнится по ответственному и вычисления не будут выполняться.
      contract.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      contract.ResponsibleEmployee = responsible;
      
      // Дата и номер.
      this.FillDocumentRegistrationData(contract, documentInfo, ArioGrammars.DocumentFact.Name, Docflow.Resources.DocumentWithoutNumber);
    }

    /// <summary>
    /// Заполнить свойства доп. соглашения по результатам обработки Ario. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку поступивших документов.</param>
    [Public]
    public virtual void FillSupAgreementPropertiesFuzzy(Contracts.ISupAgreement supAgreement,
                                                        IDocumentInfo documentInfo,
                                                        Sungero.Company.IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      
      // Вид документа.
      this.FillDocumentKind(supAgreement);
      
      this.FillContractualDocumentAmountAndCurrency(supAgreement, documentInfo);
      
      // Заполнить данные нашей стороны и корреспондента.
      this.FillContractualDocumentPartiesFuzzy(supAgreement, documentInfo, responsible);
      
      // Договор (документ-основание).
      this.FillSupAgreementLeadingDocumentFuzzy(supAgreement, arioDocument);
      
      // Заполнить ответственного после заполнения НОР и КА, чтобы вычислялась НОР из фактов, а не по отв.
      // Если так не сделать, то НОР заполнится по ответственному и вычисления не будут выполняться.
      supAgreement.Department = Company.PublicFunctions.Department.GetDepartment(responsible);
      supAgreement.ResponsibleEmployee = responsible;
      
      // Дата и номер.
      this.FillDocumentRegistrationData(supAgreement, documentInfo, ArioGrammars.SupAgreementFact.Name, Docflow.Resources.DocumentWithoutNumber);
    }

    /// <summary>
    /// Заполнить стороны договорного документа с использованием нечеткого поиска.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <param name="responsible">Ответственный за верификацию.</param>
    [Public]
    public virtual void FillContractualDocumentPartiesFuzzy(Contracts.IContractualDocument contractualDocument,
                                                            IDocumentInfo documentInfo,
                                                            IEmployee responsible)
    {
      var arioDocument = documentInfo.ArioDocument;
      var props = contractualDocument.Info.Properties;
      var businessUnitPropertyName = props.BusinessUnit.Name;
      var counterpartyPropertyName = props.Counterparty.Name;
      
      var signatoryFieldNames = new List<string>
      {
        ArioGrammars.CounterpartyFact.SignatorySurnameField,
        ArioGrammars.CounterpartyFact.SignatoryNameField,
        ArioGrammars.CounterpartyFact.SignatoryPatrnField
      };
      
      // Задать приоритет полей для поиска корреспондента. Вес определяет как учитывается вероятность каждого поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.CounterpartyFact.TinField, 0.30 },
        { ArioGrammars.CounterpartyFact.TrrcField, 0.05 },
        { ArioGrammars.CounterpartyFact.NameField, 0.25 },
        { ArioGrammars.CounterpartyFact.LegalFormField, 0.05 },
        { ArioGrammars.CounterpartyFact.SignatorySurnameField, 0.20 },
        { ArioGrammars.CounterpartyFact.SignatoryNameField, 0.10 },
        { ArioGrammars.CounterpartyFact.SignatoryPatrnField, 0.05 }
      };
      
      var defaultBusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);
      // Если НОР не указана в карточке ответственного, проверить, является ли НОР единственной в справочнике.
      var allBusinessUnits = Sungero.Company.BusinessUnits.GetAll();
      if (allBusinessUnits.Count() == 1)
        defaultBusinessUnit = allBusinessUnits.Single();
      var defaultBusinessUnitId = defaultBusinessUnit != null ? defaultBusinessUnit.Id : 0;
      
      var allCounterparyFacts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(arioDocument.Facts, ArioGrammars.CounterpartyFact.Name, weightedFields);
      var resultsForBusinessUnit = new List<IArioFactElasticsearchData>();
      var resultsForCounterparty = new List<IArioFactElasticsearchData>();
      foreach (var fact in allCounterparyFacts)
      {
        var searchResultForBusinessUnit = this.SearchBusinessUnitFuzzy(fact);
        if (searchResultForBusinessUnit.EntityId != 0)
        {
          var recognizedFields = new Dictionary<IArioFactField, double>();
          foreach (var field in searchResultForBusinessUnit.FoundedFields)
            recognizedFields.Add(field, weightedFields[field.Name]);
          searchResultForBusinessUnit.AggregateFieldsProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
          resultsForBusinessUnit.Add(searchResultForBusinessUnit);
        }
        
        var searchResultForCounterparty = this.SearchCounterpartyFuzzy(fact);
        if (searchResultForCounterparty.EntityId != 0)
        {
          // Если запись закрыта, установить минимальную вероятность. Иначе рассчитать средневзвешенную вероятность.
          var recognizedFields = new Dictionary<IArioFactField, double>();
          foreach (var field in searchResultForCounterparty.FoundedFields)
            recognizedFields.Add(field, weightedFields[field.Name]);
          searchResultForCounterparty.AggregateFieldsProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
          resultsForCounterparty.Add(searchResultForCounterparty);
        }
      }

      var recognizedBusinessUnitInfo = ArioFactElasticsearchData.Create();
      recognizedBusinessUnitInfo.EntityId = 0;
      // Если найденная НОР соответствует НОР по умолчанию.
      var foundedDefaultBusinessUnit = resultsForBusinessUnit.Where(l => l.EntityId == defaultBusinessUnitId);
      if (foundedDefaultBusinessUnit.Any())
        recognizedBusinessUnitInfo = foundedDefaultBusinessUnit.FirstOrDefault();
      // Взять первую НОР из найденных, если нет НОР по умолчанию.
      if (resultsForBusinessUnit.Any() && recognizedBusinessUnitInfo.EntityId == 0)
        recognizedBusinessUnitInfo = resultsForBusinessUnit.FirstOrDefault();

      if (recognizedBusinessUnitInfo.EntityId != 0)
      {
        contractualDocument.BusinessUnit = Company.BusinessUnits.GetAll(x => x.Id == recognizedBusinessUnitInfo.EntityId).FirstOrDefault();
        
        var businessUnitFieldNames = recognizedBusinessUnitInfo.FoundedFields.Select(l => l.Name).ToList();
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                recognizedBusinessUnitInfo.Fact,
                                                                                businessUnitFieldNames,
                                                                                businessUnitPropertyName,
                                                                                contractualDocument.BusinessUnit,
                                                                                recognizedBusinessUnitInfo.AggregateFieldsProbability,
                                                                                null);
      }

      // Заполнить подписанта.
      var recognizedBusinessUnit = RecognizedCounterparty.Create(contractualDocument.BusinessUnit,
                                                                 null,
                                                                 recognizedBusinessUnitInfo.Fact,
                                                                 string.Empty,
                                                                 recognizedBusinessUnitInfo.AggregateFieldsProbability,
                                                                 null);
      var ourSignatory = this.GetSignatoryForContractualDocumentFuzzy(contractualDocument,
                                                                      arioDocument.Facts,
                                                                      recognizedBusinessUnit,
                                                                      signatoryFieldNames,
                                                                      true);
      if (ourSignatory.Employee != null)
        this.FillOurSignatoryForContractualDocument(contractualDocument, documentInfo, ourSignatory, signatoryFieldNames);

      if (contractualDocument.BusinessUnit == null)
      {
        contractualDocument.BusinessUnit = Company.BusinessUnits.GetAll(x => x.Id == defaultBusinessUnitId).FirstOrDefault();
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                null,
                                                                                null,
                                                                                businessUnitPropertyName,
                                                                                contractualDocument.BusinessUnit,
                                                                                Module.PropertyProbabilityLevels.Min,
                                                                                null);
      }

      // Заполнить данные контрагента.
      var recognizedCounterpartyInfo = resultsForCounterparty
        .Where(x => x.EntityId != 0 && (contractualDocument.BusinessUnit == null || (contractualDocument.BusinessUnit != null &&
                                                                                     x.EntityId != contractualDocument.BusinessUnit.Company.Id)))
        .FirstOrDefault();

      if (recognizedCounterpartyInfo != null)
      {
        contractualDocument.Counterparty = Parties.Counterparties.GetAll(x => Equals(x.Id, recognizedCounterpartyInfo.EntityId)).FirstOrDefault();
        var counterpartyFieldNames = recognizedCounterpartyInfo.FoundedFields.Select(l => l.Name).ToList();
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                recognizedCounterpartyInfo.Fact,
                                                                                counterpartyFieldNames,
                                                                                counterpartyPropertyName,
                                                                                contractualDocument.Counterparty,
                                                                                recognizedCounterpartyInfo.AggregateFieldsProbability,
                                                                                null);
      }
      // Заполнить подписанта от КА.
      var recognizedCounterparty = RecognizedCounterparty.Create(null,
                                                                 contractualDocument.Counterparty,
                                                                 recognizedCounterpartyInfo != null ? recognizedCounterpartyInfo.Fact : null,
                                                                 string.Empty,
                                                                 null,
                                                                 recognizedCounterpartyInfo != null ? recognizedCounterpartyInfo.AggregateFieldsProbability : 0);
      
      var signedBy = this.GetSignatoryForContractualDocumentFuzzy(contractualDocument,
                                                                  arioDocument.Facts,
                                                                  recognizedCounterparty,
                                                                  signatoryFieldNames,
                                                                  false);
      
      // При заполнении поля подписал, если контрагент не заполнен, он подставляется из подписанта.
      if (signedBy.Contact != null)
        this.FillCounterpartySignatoryForContractualDocument(contractualDocument, documentInfo, signedBy, signatoryFieldNames);
    }

    /// <summary>
    /// Заполнить ведущий договор финансового документа.
    /// </summary>
    /// <param name="document">Финансовый документ.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    [Public]
    public virtual void FillAccountingDocumentLeadingDocumentFuzzy(IAccountingDocumentBase document, IArioDocument arioDocument)
    {
      var recognizedDocument = this.GetLeadingContractualDocumentFuzzy(arioDocument.Facts, document.BusinessUnit, document.Counterparty);
      if (recognizedDocument.Contract != null)
      {
        document.LeadingDocument = recognizedDocument.Contract;
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                                  recognizedDocument.Fact,
                                                                                  null,
                                                                                  document.Info.Properties.LeadingDocument.Name,
                                                                                  document.LeadingDocument,
                                                                                  recognizedDocument.Probability);
      }
    }
    
    /// <summary>
    /// Заполнить ведущий договор во входящем счете.
    /// </summary>
    /// <param name="incomingInvoice">Входящий счет.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    [Public]
    public virtual void FillIncomingInvoiceContractFuzzy(IIncomingInvoice incomingInvoice, IArioDocument arioDocument)
    {
      var recognizedDocument = this.GetLeadingContractualDocumentFuzzy(arioDocument.Facts, incomingInvoice.BusinessUnit, incomingInvoice.Counterparty);
      if (recognizedDocument.Contract != null)
      {
        incomingInvoice.Contract = recognizedDocument.Contract;
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                                  recognizedDocument.Fact,
                                                                                  null,
                                                                                  incomingInvoice.Info.Properties.Contract.Name,
                                                                                  incomingInvoice.Contract,
                                                                                  recognizedDocument.Probability);
      }
    }
    
    /// <summary>
    /// Заполнить ведущий договор в доп. соглашении.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    [Public]
    public virtual void FillSupAgreementLeadingDocumentFuzzy(Contracts.ISupAgreement supAgreement, IArioDocument arioDocument)
    {
      // Получить факты, содержащие номер и/или дату договора. Приоритетнее факты, содержащие оба поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.SupAgreementFact.DocumentBaseDateField, 0.5 },
        { ArioGrammars.SupAgreementFact.DocumentBaseNumberField, 0.5 }
      };
      var facts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(arioDocument.Facts, ArioGrammars.SupAgreementFact.Name, weightedFields);

      if (!facts.Any())
        return;
      
      // Подготовить список договоров для поска. НОР обязательна, контрагент - нет.
      if (supAgreement.BusinessUnit == null)
        return;
      
      var contracts = Contracts.ContractBases.GetAll(d => Equals(d.BusinessUnit, supAgreement.BusinessUnit));
      if (supAgreement.Counterparty != null)
        contracts = contracts.Where(d => Equals(d.Counterparty, supAgreement.Counterparty));
      if (!contracts.Any())
        return;
      
      // Найти договор по номеру и дате.
      var recognizedDocument = this.GetRecognizedDocumentFuzzy(facts, contracts, ArioGrammars.SupAgreementFact.DocumentBaseNumberField,
                                                               ArioGrammars.SupAgreementFact.DocumentBaseDateField);
      
      if (recognizedDocument.Document != null)
      {
        supAgreement.LeadingDocument = Contracts.ContractBases.As(recognizedDocument.Document);
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.LinkFactFieldsAndProperty(arioDocument.RecognitionInfo,
                                                                                        recognizedDocument.Fact,
                                                                                        weightedFields.Select(fl => fl.Key).ToList(),
                                                                                        supAgreement.Info.Properties.LeadingDocument.Name,
                                                                                        supAgreement.LeadingDocument,
                                                                                        recognizedDocument.Probability,
                                                                                        null);
      }
    }

    /// <summary>
    /// Получить подписанта нашей стороны/подписанта контрагента для договорного документа по фактам и НОР с использованием нечеткого поиска.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="recognizedOrganization">Структура с НОР, КА, фактом и признаком доверия.</param>
    /// <param name="signatoryFieldNames">Список наименований полей с ФИО подписанта.</param>
    /// <param name="isOurSignatory">Признак поиска нашего подписанта (true) или подписанта КА (false).</param>
    /// <returns>Структура, содержащая сотрудника или контакт, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetSignatoryForContractualDocumentFuzzy(Contracts.IContractualDocument document,
                                                                               List<IArioFact> facts,
                                                                               IRecognizedCounterparty recognizedOrganization,
                                                                               List<string> signatoryFieldNames,
                                                                               bool isOurSignatory = false)
    {
      var props = document.Info.Properties;
      var signatoryFacts = Commons.PublicFunctions.Module.GetFacts(facts, ArioGrammars.CounterpartyFact.Name);
      var signedBy = RecognizedOfficial.Create(null, null, null, Module.PropertyProbabilityLevels.Min);
      
      if (!signatoryFacts.Any())
        return signedBy;
      
      if (recognizedOrganization != null)
      {
        var organizationFact = recognizedOrganization.Fact;
        if (organizationFact != null)
        {
          signedBy.Fact = organizationFact;
          var isOrganizationFactWithSignatory = Commons.PublicFunctions.Module.GetFields(organizationFact, signatoryFieldNames).Any();
          
          var recognizedOrganizationNaming = this.GetRecognizedPersonNaming(organizationFact,
                                                                            ArioGrammars.CounterpartyFact.SignatorySurnameField,
                                                                            ArioGrammars.CounterpartyFact.SignatoryNameField,
                                                                            ArioGrammars.CounterpartyFact.SignatoryPatrnField);
          if (isOrganizationFactWithSignatory)
            return isOurSignatory
              ? this.GetRecognizedOurSignatoryForContractualDocumentFuzzy(document, organizationFact, recognizedOrganizationNaming)
              : this.GetRecognizedContactFuzzy(organizationFact, document.Counterparty, recognizedOrganizationNaming);
        }
      }
      
      signatoryFacts = signatoryFacts
        .Where(f => Commons.PublicFunctions.Module.GetFields(f, signatoryFieldNames).Any()).ToList();
      
      var organizationSignatory = new List<IRecognizedOfficial>();
      foreach (var signatoryFact in signatoryFacts)
      {
        IRecognizedOfficial signatory = null;
        
        var recognizedSignatoryNaming = this.GetRecognizedPersonNaming(signatoryFact,
                                                                       ArioGrammars.CounterpartyFact.SignatorySurnameField,
                                                                       ArioGrammars.CounterpartyFact.SignatoryNameField,
                                                                       ArioGrammars.CounterpartyFact.SignatoryPatrnField);
        if (isOurSignatory)
        {
          signatory = this.GetRecognizedOurSignatoryForContractualDocumentFuzzy(document, signatoryFact, recognizedSignatoryNaming);
          if (signatory.Employee != null)
            organizationSignatory.Add(signatory);
        }
        else
        {
          signatory = this.GetRecognizedContactFuzzy(signatoryFact, document.Counterparty, recognizedSignatoryNaming);
          if (signatory.Contact != null)
            organizationSignatory.Add(signatory);
        }
      }
      
      if (organizationSignatory.Any())
        signedBy = organizationSignatory.OrderByDescending(x => x.Probability).FirstOrDefault();

      return signedBy;
    }

    /// <summary>
    /// Получить контактное лицо по данным из факта и контрагента с использованием нечеткого поиска.
    /// </summary>
    /// <param name="contactFact">Факт, содержащий сведения о контакте.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="recognizedContactNaming">Полное и краткое ФИО персоны.</param>
    /// <returns>Структура, содержащая контактное лицо, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetRecognizedContactFuzzy(IArioFact contactFact,
                                                                 ICounterparty counterparty,
                                                                 IRecognizedPersonNaming recognizedContactNaming)
    {
      var signedBy = RecognizedOfficial.Create(null, Sungero.Parties.Contacts.Null, contactFact, Module.PropertyProbabilityLevels.Min);
      
      if (contactFact == null)
        return signedBy;
      
      var contact = Contacts.Null;
      
      var fullName = recognizedContactNaming.FullName;
      var shortName = recognizedContactNaming.ShortName;
      var probability = Module.PropertyProbabilityLevels.Min;
      
      if (!string.IsNullOrWhiteSpace(fullName) || !string.IsNullOrWhiteSpace(shortName))
      {
        var counterpartyId = counterparty != null ? counterparty.Id : 0;
        contact = Parties.PublicFunctions.Contact.GetContactsByNameFuzzy(fullName, counterpartyId);
        
        if (contact == null)
          contact = Parties.PublicFunctions.Contact.GetContactsByNameFuzzy(shortName, counterpartyId);
        else
          probability = Module.PropertyProbabilityLevels.UpperMiddle;
      }
      
      if (contact == null)
        return signedBy;
      
      signedBy.Contact = contact;
      signedBy.Fact = contactFact;
      signedBy.Probability = probability;
      
      return signedBy;
    }

    /// <summary>
    /// Получить подписанта нашей стороны для договорного документа по факту с использованием нечеткого поиска.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="ourSignatoryFact">Факт, содержащий сведения о подписанте нашей стороны.</param>
    /// <param name="recognizedOurSignatoryNaming">Полное и краткое ФИО подписанта нашей стороны.</param>
    /// <returns>Структура, содержащая сотрудника, факт и вероятность.</returns>
    [Public]
    public virtual IRecognizedOfficial GetRecognizedOurSignatoryForContractualDocumentFuzzy(Contracts.IContractualDocument document,
                                                                                            IArioFact ourSignatoryFact,
                                                                                            IRecognizedPersonNaming recognizedOurSignatoryNaming)
    {
      var signedBy = RecognizedOfficial.Create(null, null, ourSignatoryFact, Module.PropertyProbabilityLevels.Min);
      var businessUnit = document.BusinessUnit;

      if (ourSignatoryFact == null)
        return signedBy;
      
      var fullName = recognizedOurSignatoryNaming.FullName;
      var shortName = recognizedOurSignatoryNaming.ShortName;
      var foundEmployees = Company.PublicFunctions.Employee.GetEmployeesByNameFuzzy(fullName, businessUnit != null ? businessUnit.Id : 0);
      
      if (!foundEmployees.Any())
        return signedBy;
      
      if (foundEmployees.Count == 1)
      {
        signedBy.Employee = foundEmployees.FirstOrDefault();
        
        var employeeFoundByFullName = string.Equals(signedBy.Employee.Name, fullName, StringComparison.InvariantCultureIgnoreCase);
        var fullNameEqualsShortName = string.Equals(fullName, shortName, StringComparison.InvariantCultureIgnoreCase);
        if (employeeFoundByFullName && !fullNameEqualsShortName)
          signedBy.Probability = Constants.Module.PropertyProbabilityLevels.Max;
        else
          signedBy.Probability = Constants.Module.PropertyProbabilityLevels.LowerMiddle;
      }
      
      return signedBy;
    }

    #endregion

    #region Заполнение даты и номера

    /// <summary>
    /// Заполнить номер и дату документа по результатам обработки Ario.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="arioDocument">Информация о документе Ario.</param>
    /// <param name="factName">Наименование факта.</param>
    /// <param name="numberPropertyInfo">Информация о свойстве с номером.</param>
    /// <param name="numberFieldName">Наименование поля с номером.</param>
    /// <param name="datePropertyInfo">Информация о свойстве с датой.</param>
    /// <param name="dateFieldName">Наименование поля с датой.</param>
    [Public]
    public virtual void FillDocumentNumberAndDateFuzzy(IOfficialDocument document, IArioDocument arioDocument, string factName,
                                                       Sungero.Domain.Shared.IStringPropertyInfo numberPropertyInfo, string numberFieldName,
                                                       Sungero.Domain.Shared.IDateTimePropertyInfo datePropertyInfo, string dateFieldName)
    {
      // Получить факты с номером и/или датой документа. Приоритетнее факты, содержащие оба поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { numberFieldName, 0.5 },
        { dateFieldName, 0.5 }
      };
      var facts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(arioDocument.Facts, factName, weightedFields);
      
      // Заполнить номер.
      var numberFact = facts.FirstOrDefault(f => f.Fields.Any(fl => fl.Name == numberFieldName));
      if (numberFact != null)
      {
        var recognizedNumber = this.GetRecognizedNumber(new List<IArioFact>() { numberFact }, factName, numberFieldName, numberPropertyInfo);
        document.SetPropertyValue(numberPropertyInfo.Name, recognizedNumber.Number);
        Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                          recognizedNumber.Fact,
                                                                          numberFieldName,
                                                                          numberPropertyInfo.Name,
                                                                          recognizedNumber.Number,
                                                                          recognizedNumber.Probability);
      }
      
      // Заполнить дату.
      var dateFact = facts.FirstOrDefault(f => f.Fields.Any(fl => fl.Name == dateFieldName));
      if (dateFact != null)
      {
        var recognizedDate = this.GetRecognizedDate(new List<IArioFact>() { dateFact }, factName, dateFieldName);
        if (recognizedDate.Fact != null)
        {
          document.SetPropertyValue(datePropertyInfo.Name, recognizedDate.Date);
          Commons.PublicFunctions.EntityRecognitionInfo.LinkFactAndProperty(arioDocument.RecognitionInfo,
                                                                            recognizedDate.Fact,
                                                                            dateFieldName,
                                                                            datePropertyInfo.Name,
                                                                            recognizedDate.Date,
                                                                            recognizedDate.Probability);
        }
      }
    }

    #endregion

    #region Получение ведущего документа

    /// <summary>
    /// Получить ведущий договор из фактов.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Структура, содержащая ведущий договорной документ, факт и признак доверия.</returns>
    [Public]
    public virtual IRecognizedContract GetLeadingContractualDocumentFuzzy(List<IArioFact> facts, IBusinessUnit businessUnit, ICounterparty counterparty)
    {
      var result = RecognizedContract.Create(Contracts.ContractualDocuments.Null, null, null);
      if (!facts.Any())
        return result;

      var fieldNames = new List<string>()
      {
        ArioGrammars.FinancialDocumentFact.DocumentBaseNameField,
        ArioGrammars.FinancialDocumentFact.DocumentBaseDateField
      };
      
      var baseDocumentFacts = facts
        .Where(f => f.Name == ArioGrammars.FinancialDocumentFact.Name &&
               f.Fields.Any(fl => fieldNames.Contains(fl.Name) && !string.IsNullOrEmpty(fl.Value)))
        .ToList();

      if (!baseDocumentFacts.Any())
        return result;

      // Получить список договоров для поиска. НОР обязательна, контрагент - нет.
      if (businessUnit == null)
        return result;

      var contractualDocuments = Contracts.ContractualDocuments.GetAll(d => Equals(d.BusinessUnit, businessUnit));
      if (counterparty != null)
        contractualDocuments = contractualDocuments.Where(d => Equals(d.Counterparty, counterparty));
      if (!contractualDocuments.Any())
        return result;
      
      // Если найдено несколько документов-оснований, отсортировать их по важности. Более приоритетные содержат слово "договор" в названии.
      if (baseDocumentFacts.Count > 1)
      {
        var contractualBaseNames = Resources.ContractualBaseDocumentNamesPriority.ToString().Split(';');
        
        var factScores = new Dictionary<IArioFact, double>();
        foreach (var fact in baseDocumentFacts)
        {
          var baseNumberDateFields = fact.Fields.Where(fl => fieldNames.Contains(fl.Name) && !string.IsNullOrEmpty(fl.Value)).ToList();
          var factProbability = baseNumberDateFields.Sum(fl => fl.Probability) / 2;
          var score = Math.Round(baseNumberDateFields.Count + (factProbability / 100), 4);
          var baseDocumentNameField = fact.Fields.FirstOrDefault(fl => fl.Name == ArioGrammars.FinancialDocumentFact.DocumentBaseNameField);
          if (baseDocumentNameField != null)
          {
            var baseDocumentNameFieldValue = baseDocumentNameField.Value.Trim().ToLower();
            for (var i = 0; i < contractualBaseNames.Length; i++)
            {
              if (baseDocumentNameFieldValue.Contains(contractualBaseNames[i]))
              {
                score += 10 * (contractualBaseNames.Length - i);
                if (baseDocumentNameFieldValue.StartsWith(contractualBaseNames[i]))
                  score += 100;
                break;
              }
            }
          }
          factScores.Add(fact, score);
        }
        baseDocumentFacts = factScores.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
      }

      // Найти документ по номеру и дате.
      var recognizedDocument = this.GetRecognizedDocumentFuzzy(baseDocumentFacts,
                                                               contractualDocuments,
                                                               ArioGrammars.FinancialDocumentFact.DocumentBaseNumberField,
                                                               ArioGrammars.FinancialDocumentFact.DocumentBaseDateField);
      
      if (recognizedDocument.Document != null)
      {
        // Создать копию факта для подсветки полей документа-основания.
        var fieldsForLink = recognizedDocument.Fact.Fields.Where(fl => fieldNames.Contains(fl.Name)).ToList();
        result.Fact = ArioFact.Create(recognizedDocument.Fact.Id, recognizedDocument.Fact.Name, fieldsForLink);
        result.Contract = Contracts.ContractualDocuments.As(recognizedDocument.Document);
        result.Probability = recognizedDocument.Probability;
      }
      else if (counterparty != null)
      {
        // Если договор по номеру и дате не нашли, но есть единственный действующий для указанного контрагента, то подставить его.
        contractualDocuments = contractualDocuments.Where(x => x.LifeCycleState.HasValue &&
                                                          x.LifeCycleState.Value == Contracts.ContractualDocument.LifeCycleState.Active);
        if (contractualDocuments.Count() == 1)
        {
          result.Contract = contractualDocuments.Single();
          result.Probability = Module.PropertyProbabilityLevels.Min;
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить документ из фактов по номеру и дате.
    /// </summary>
    /// <param name="orderedFacts">Список фактов, отсортированных для поиска документа.</param>
    /// <param name="filteredDocuments">Предварительно отфильтрованный запрос документов.</param>
    /// <param name="numberFieldName">Имя поля факта с номером документа.</param>
    /// <param name="dateFieldName">Имя поля факта с датой документа.</param>
    /// <returns>Распознанный документ.</returns>
    [Public]
    public virtual IRecognizedDocument GetRecognizedDocumentFuzzy(List<IArioFact> orderedFacts, IQueryable<IOfficialDocument> filteredDocuments,
                                                                  string numberFieldName, string dateFieldName)
    {
      var recognizedDocument = RecognizedDocument.Create();
      if (!orderedFacts.Any())
        return recognizedDocument;

      if (!filteredDocuments.Any())
        return recognizedDocument;

      foreach (var fact in orderedFacts)
      {
        var probability = 0d;
        var responseToDateField = fact.Fields
          .Where(fl => fl.Name == dateFieldName && !string.IsNullOrWhiteSpace(fl.Value))
          .OrderByDescending(fl => fl.Probability)
          .FirstOrDefault();
        if (responseToDateField != null)
        {
          DateTime responseToDate;
          if (Calendar.TryParseDate(responseToDateField.Value, out responseToDate))
          {
            filteredDocuments = filteredDocuments.Where(l => Equals(l.RegistrationDate, responseToDate));
            probability += responseToDateField.Probability;
          }
        }
        
        var responseToNumberField = fact.Fields
          .Where(fl => fl.Name == numberFieldName && !string.IsNullOrWhiteSpace(fl.Value))
          .OrderByDescending(fl => fl.Probability)
          .FirstOrDefault();
        if (responseToNumberField != null)
        {
          filteredDocuments = filteredDocuments.Where(l => string.Equals(l.RegistrationNumber, responseToNumberField.Value, StringComparison.InvariantCultureIgnoreCase));
          probability += responseToNumberField.Probability;
        }

        if (filteredDocuments.Count() == 1)
        {
          recognizedDocument.Document = filteredDocuments.First();
          recognizedDocument.Fact = fact;
          recognizedDocument.Probability = probability / 2;
          break;
        }
      }

      return recognizedDocument;
    }

    #endregion

    #region Получение контрагентов, контактов, подписантов, НОР
    /// <summary>
    /// Определить направление документа, НОР и КА у счет-фактуры. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат подбора сторон сделки для документа.</returns>
    /// <remarks>Если НОР выступает продавцом, то счет-фактура - исходящая, иначе - входящая.</remarks>
    [Public]
    public virtual IRecognizedDocumentParties GetRecognizedTaxInvoicePartiesFuzzy(List<IArioFact> facts, IEmployee responsible)
    {
      // Определить является НОР продавцом (грузоотправителем) или покупателем (грузополучателем).
      var buyer = this.GetRecognizedBusinessUnitFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
      if (buyer == null)
        buyer = this.GetRecognizedBusinessUnitFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);
      var buyerIsBusinessUnit = buyer != null && buyer.BusinessUnit != null;
      
      var seller = this.GetRecognizedBusinessUnitFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
      if (seller == null)
        seller = this.GetRecognizedBusinessUnitFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      var sellerIsBusinessUnit = seller != null && seller.BusinessUnit != null;

      // Получить контргаента в зависимости от результатов поиска НОР.
      if (!buyerIsBusinessUnit)
      {
        buyer = this.GetRecognizedCounterpartyFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Buyer);
        if (buyer == null)
          buyer = this.GetRecognizedCounterpartyFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Consignee);
      }
      if (!sellerIsBusinessUnit)
      {
        seller = this.GetRecognizedCounterpartyFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Seller);
        if (seller == null)
          seller = this.GetRecognizedCounterpartyFuzzy(facts, ArioGrammars.CounterpartyFact.CounterpartyTypes.Shipper);
      }
      
      // Определить направление документа.
      var recognizedDocumentParties = RecognizedDocumentParties.Create();
      recognizedDocumentParties.ResponsibleEmployeeBusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(responsible);

      if (buyerIsBusinessUnit && sellerIsBusinessUnit)
      {
        // Мультинорность. Уточнить НОР по ответственному.
        if (Equals(seller.BusinessUnit, recognizedDocumentParties.ResponsibleEmployeeBusinessUnit))
        {
          // Исходящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = true;
          recognizedDocumentParties.Counterparty = buyer;
          recognizedDocumentParties.BusinessUnit = seller;
        }
        else
        {
          // Входящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = false;
          recognizedDocumentParties.Counterparty = seller;
          recognizedDocumentParties.BusinessUnit = buyer;
        }
      }
      else if (buyerIsBusinessUnit)
      {
        // Входящий документ.
        recognizedDocumentParties.IsDocumentOutgoing = false;
        recognizedDocumentParties.Counterparty = seller;
        recognizedDocumentParties.BusinessUnit = buyer;
      }
      else if (sellerIsBusinessUnit)
      {
        // Исходящий документ.
        recognizedDocumentParties.IsDocumentOutgoing = true;
        recognizedDocumentParties.Counterparty = buyer;
        recognizedDocumentParties.BusinessUnit = seller;
      }
      else
      {
        // НОР не найдена по фактам - НОР будет взята по ответственному.
        if (buyer != null && buyer.Counterparty != null && (seller == null || seller.Counterparty == null))
        {
          // Исходящий документ, потому что buyer - контрагент, а другой информации нет.
          recognizedDocumentParties.IsDocumentOutgoing = true;
          recognizedDocumentParties.Counterparty = buyer;
        }
        else
        {
          // Входящий документ.
          recognizedDocumentParties.IsDocumentOutgoing = false;
          recognizedDocumentParties.Counterparty = seller;
        }
      }

      return recognizedDocumentParties;
    }

    /// <summary>
    /// Поиск контрагента по извлеченным фактам. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="counterpartyType">Тип контрагента.</param>
    /// <returns>Контрагент со связанным фактом.</returns>
    [Public]
    public virtual IRecognizedCounterparty GetRecognizedCounterpartyFuzzy(List<IArioFact> facts, string counterpartyType)
    {
      var recognizedCounterparty = RecognizedCounterparty.Create();

      // Задать приоритет полей для поиска. Вес определяет как учитывается вероятность каждого поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.CounterpartyFact.TinField, 0.40 },
        { ArioGrammars.CounterpartyFact.TrrcField, 0.05 },
        { ArioGrammars.CounterpartyFact.PsrnField, 0.30 },
        { ArioGrammars.CounterpartyFact.NameField, 0.20 },
        { ArioGrammars.CounterpartyFact.LegalFormField, 0.05 }
      };
      
      // Отфильтровать факты по типу организации.
      var counterpartyFacts = facts
        .Where(f => f.Name == ArioGrammars.CounterpartyFact.Name &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.CounterpartyFact.CounterpartyTypeField && fl.Value == counterpartyType) &&
               f.Fields.Any(fl => weightedFields.ContainsKey(fl.Name) && !string.IsNullOrEmpty(fl.Value)))
        .ToList();
      
      if (!counterpartyFacts.Any())
        return recognizedCounterparty;

      // Отсортировать факты по количеству и приоритету полей.
      var orderedCounterpartyFacts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(counterpartyFacts, ArioGrammars.CounterpartyFact.Name, weightedFields);
      
      // Получить контрагента по значениям полей факта с использованием Elasticsearch.
      foreach (var fact in orderedCounterpartyFacts)
      {
        var searchResult = this.SearchCounterpartyFuzzy(fact);

        if (searchResult.EntityId == 0)
          continue;
        
        var counterparty = Parties.Counterparties.GetAll(x => Equals(x.Id, searchResult.EntityId)).FirstOrDefault();
        if (counterparty == null)
          continue;
        
        fact.Fields = searchResult.FoundedFields;
        recognizedCounterparty.Fact = fact;
        recognizedCounterparty.Type = counterpartyType;
        recognizedCounterparty.Counterparty = counterparty;
        
        var recognizedFields = new Dictionary<IArioFactField, double>();
        foreach (var field in fact.Fields)
        {
          recognizedFields.Add(field, weightedFields[field.Name]);
        }
        recognizedCounterparty.CounterpartyProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
        
        break;
      }
      
      return recognizedCounterparty;
    }

    /// <summary>
    /// Поиск НОР по извлеченным фактам. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="facts">Извлеченные из документа факты.</param>
    /// <param name="counterpartyType">Тип факта с НОР.</param>
    /// <returns>НОР со связанным фактом.</returns>
    [Public]
    public virtual IRecognizedCounterparty GetRecognizedBusinessUnitFuzzy(List<IArioFact> facts, string counterpartyType)
    {
      var recognizedBusinessUnit = RecognizedCounterparty.Create();
      
      // Задать приоритет полей для поиска. Вес определяет как учитывается вероятность каждого поля.
      var weightedFields = new Dictionary<string, double>()
      {
        { ArioGrammars.CounterpartyFact.TinField, 0.40 },
        { ArioGrammars.CounterpartyFact.TrrcField, 0.05 },
        { ArioGrammars.CounterpartyFact.PsrnField, 0.30 },
        { ArioGrammars.CounterpartyFact.NameField, 0.20 },
        { ArioGrammars.CounterpartyFact.LegalFormField, 0.05 }
      };
      
      // Отфильтровать факты по типу организации.
      var counterpartyFacts = facts
        .Where(f => f.Name == ArioGrammars.CounterpartyFact.Name &&
               f.Fields.Any(fl => fl.Name == ArioGrammars.CounterpartyFact.CounterpartyTypeField && fl.Value == counterpartyType) &&
               f.Fields.Any(fl => weightedFields.ContainsKey(fl.Name) && !string.IsNullOrEmpty(fl.Value)))
        .ToList();
      
      if (!counterpartyFacts.Any())
        return recognizedBusinessUnit;

      // Отсортировать факты по количеству и приоритету полей.
      var orderedCounterpartyFacts = Commons.PublicFunctions.Module.GetOrderedFactsByFieldPriorities(counterpartyFacts, ArioGrammars.CounterpartyFact.Name, weightedFields);
      
      // Получить НОР по значениям полей факта с использованием Elasticsearch.
      foreach (var fact in orderedCounterpartyFacts)
      {
        var searchResult = this.SearchBusinessUnitFuzzy(fact);

        if (searchResult.EntityId == 0)
          continue;
        
        var businessUnit = Company.BusinessUnits.GetAll(x => Equals(x.Id, searchResult.EntityId)).FirstOrDefault();
        if (businessUnit == null)
          continue;
        
        fact.Fields = searchResult.FoundedFields;
        recognizedBusinessUnit.Fact = fact;
        recognizedBusinessUnit.Type = counterpartyType;
        recognizedBusinessUnit.BusinessUnit = businessUnit;
        
        var recognizedFields = new Dictionary<IArioFactField, double>();
        foreach (var field in fact.Fields)
        {
          recognizedFields.Add(field, weightedFields[field.Name]);
        }
        recognizedBusinessUnit.BusinessUnitProbability = Commons.PublicFunctions.Module.GetAggregateFieldsProbability(recognizedFields);
        
        break;
      }
      
      return recognizedBusinessUnit;
    }

    /// <summary>
    /// Найти корреспондента письма по значению полей факта. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <returns>Результаты поиска корреспондента.</returns>
    [Public]
    public virtual IArioFactElasticsearchData SearchLetterCorrespondentFuzzy(IArioFact fact)
    {
      // Создать список запросов для поиска по полям входящего письма.
      var fieldQueries = new List<IArioFieldElasticsearchData>();

      // ИНН.
      var tin = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TinField);
      if (tin != null)
      {
        // Проверить ИНН на валидность. По значению поля Ario, либо (если поле извлечено не было), с помощью функции справочника.
        var tinIsValid = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TinIsValidField);
        if ((tinIsValid != null && tinIsValid.Value == "true") || (tinIsValid == null && string.IsNullOrEmpty(Parties.PublicFunctions.Counterparty.CheckTin(tin.Value, true))))
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(tin, "TIN", ElasticsearchTypes.Term));
      }

      // КПП. Только для уточнения организаций, найденных по ИНН.
      var trrc = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TrrcField);
      if (tin != null && trrc != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(trrc, "TRRC", ElasticsearchTypes.Term, true));
      
      // ОГРН.
      var psrn = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.PsrnField);
      if (psrn != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(psrn, "PSRN", ElasticsearchTypes.Term));
      
      // Наименование.
      var names = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.LetterFact.CorrespondentNameField });
      if (names != null)
      {
        // Если есть несколько полей с наименованием, проверить их все, последовательно уточняя. Если не нашли по полному наименованию, искать по краткому.
        foreach (var name in names)
        {
          var normalizedValue = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(name.Value);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.MatchAnd, normalizedValue));
          var fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Parties.PublicConstants.CompanyBase.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.MatchAnd, normalizedValue));
          fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Parties.PublicConstants.CompanyBase.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
        }
      }

      // Головная организация.
      var headCompany = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.HeadCompanyNameField);
      if (headCompany != null)
      {
        var normalizedValue = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(headCompany.Value);
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(headCompany, "HeadCompany", ElasticsearchTypes.FuzzyOr, normalizedValue));
      }
      
      // Адрес эл. почты.
      var emails = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.LetterFact.EmailField });
      if (emails != null)
      {
        foreach (var email in emails)
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(email, "Email", ElasticsearchTypes.MatchAnd));
      }
      
      // Сайт.
      var sites = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.LetterFact.WebsiteField });
      if (sites != null)
      {
        foreach (var site in sites)
        {
          var normalizedValue = Parties.PublicFunctions.Module.NormalizeSite(site.Value);
          if (!string.IsNullOrEmpty(normalizedValue))
            fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(site, "Homepage", ElasticsearchTypes.MatchAnd, normalizedValue));
        }
      }

      // Номер телефона.
      var phones = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.LetterFact.PhoneField });
      if (phones != null)
      {
        foreach (var phone in phones)
        {
          // По номеру телефона искать только для уточнения сущностей, найденных по другим полям.
          var normalizedValue = Parties.PublicFunctions.Module.NormalizePhone(phone.Value);
          if (!string.IsNullOrEmpty(normalizedValue))
            fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(phone, "Phones", ElasticsearchTypes.Wildcard, normalizedValue, true, false));
        }
      }

      // Выполнить запрос.
      var searchQuery = ArioFactElasticsearchData.Create();
      searchQuery.EntityName = CompanyBases.Info.Name;
      searchQuery.Fact = fact;
      searchQuery.Queries = fieldQueries;
      return Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(searchQuery);
    }

    /// <summary>
    /// Найти НОР письма по значению полей факта. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <returns>Результаты поиска НОР.</returns>
    [Public]
    public virtual IArioFactElasticsearchData SearchLetterBusinessUnitFuzzy(IArioFact fact)
    {
      // Создать список запросов для поиска по полям входящего письма.
      var fieldQueries = new List<IArioFieldElasticsearchData>();

      // ИНН.
      var tin = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TinField);
      if (tin != null)
      {
        // Проверить ИНН на валидность. По значению поля Ario, либо (если поле извлечено не было), с помощью функции справочника.
        var tinIsValid = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TinIsValidField);
        if ((tinIsValid != null && tinIsValid.Value == "true") || (tinIsValid == null && string.IsNullOrEmpty(Parties.PublicFunctions.Counterparty.CheckTin(tin.Value, true))))
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(tin, "TIN", ElasticsearchTypes.Term));
      }

      // КПП. Только для уточнения организаций, найденных по ИНН.
      var trrc = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.TrrcField);
      if (trrc != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(trrc, "TRRC", ElasticsearchTypes.Term, true));

      // ОГРН.
      var psrn = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.LetterFact.PsrnField);
      if (psrn != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(psrn, "PSRN", ElasticsearchTypes.Term));
      
      // Наименование.
      var names = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.LetterFact.CorrespondentNameField });
      if (names != null)
      {
        // Если есть несколько полей с наименованием, проверить их все, последовательно уточняя.
        foreach (var name in names)
        {
          var normalizedValue = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(name.Value);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.MatchAnd, normalizedValue));
          var fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Company.PublicConstants.BusinessUnit.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.MatchAnd, normalizedValue));
          fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Company.PublicConstants.BusinessUnit.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
        }
      }
      
      // Выполнить запрос.
      var searchQuery = ArioFactElasticsearchData.Create();
      searchQuery.EntityName = Company.BusinessUnits.Info.Name;
      searchQuery.Fact = fact;
      searchQuery.Queries = fieldQueries;
      return Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(searchQuery);
    }

    /// <summary>
    /// Найти контрагента по значению полей факта. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <returns>Результаты поиска контрагента.</returns>
    [Public]
    public virtual IArioFactElasticsearchData SearchCounterpartyFuzzy(IArioFact fact)
    {
      // Подготовить данные для поиска полей.
      var fieldQueries = new List<IArioFieldElasticsearchData>();
      
      // ИНН.
      var tin = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TinField);
      if (tin != null)
      {
        // Проверить ИНН на валидность. По значению поля Ario, либо (если поле извлечено не было), с помощью функции справочника.
        var tinIsValid = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TinIsValidField);
        if ((tinIsValid != null && tinIsValid.Value == "true") || (tinIsValid == null && string.IsNullOrEmpty(Parties.PublicFunctions.Counterparty.CheckTin(tin.Value, true))))
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(tin, "TIN", ElasticsearchTypes.Term));
      }
      
      // КПП. Только для уточнения организаций, найденных по ИНН.
      var trrc = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TrrcField);
      if (tin != null && trrc != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(trrc, "TRRC", ElasticsearchTypes.Term, true));

      // ОГРН.
      var psrn = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.PsrnField);
      if (psrn != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(psrn, "PSRN", ElasticsearchTypes.Term));
      
      // Наименование.
      var names = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.CounterpartyFact.NameField });
      if (names != null)
      {
        var legalForm = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.LegalFormField);
        // Если есть несколько полей с наименованием, проверить их все, последовательно уточняя. Если не нашли по полному наименованию, искать по краткому.
        foreach (var name in names)
        {
          // Если есть поле с организационно-правовой формой, добавить значение ОПФ к наименованию.
          if (legalForm != null && !name.Value.Contains(legalForm.Value))
            name.Value = string.Format("{0} {1}", legalForm.Value, name.Value);
          var normalizedValue = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(name.Value);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.MatchAnd, normalizedValue));
          var fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Parties.PublicConstants.CompanyBase.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.MatchAnd, normalizedValue));
          fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Parties.PublicConstants.CompanyBase.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
        }
      }

      // Выполнить запрос.
      var searchQuery = ArioFactElasticsearchData.Create();
      searchQuery.EntityName = Parties.CompanyBases.Info.Name;
      searchQuery.Fact = fact;
      searchQuery.Queries = fieldQueries;
      return Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(searchQuery);
    }

    /// <summary>
    /// Найти НОР по значению полей факта. С использованием нечеткого поиска.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <returns>Результаты поиска НОР.</returns>
    [Public]
    public virtual IArioFactElasticsearchData SearchBusinessUnitFuzzy(IArioFact fact)
    {
      // Подготовить данные для поиска полей.
      var fieldQueries = new List<IArioFieldElasticsearchData>();
      
      // ИНН.
      var tin = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TinField);
      if (tin != null)
      {
        // Проверить ИНН на валидность. По значению поля Ario, либо (если поле извлечено не было), с помощью функции справочника.
        var tinIsValid = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TinIsValidField);
        if ((tinIsValid != null && tinIsValid.Value == "true") || (tinIsValid == null && string.IsNullOrEmpty(Parties.PublicFunctions.Counterparty.CheckTin(tin.Value, true))))
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(tin, "TIN", ElasticsearchTypes.Term));
      }

      // КПП. Только для уточнения организаций, найденных по ИНН.
      var trrc = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.TrrcField);
      if (tin != null && trrc != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(trrc, "TRRC", ElasticsearchTypes.Term, true));

      // ОГРН.
      var psrn = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.PsrnField);
      if (psrn != null)
        fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(psrn, "PSRN", ElasticsearchTypes.Term));
      
      // Наименование.
      var names = Commons.PublicFunctions.Module.GetFields(fact, new List<string>() { ArioGrammars.CounterpartyFact.NameField });
      if (names != null)
      {
        var legalForm = Commons.PublicFunctions.Module.GetField(fact, ArioGrammars.CounterpartyFact.LegalFormField);
        // Если есть несколько полей с наименованием, проверить их все, последовательно уточняя.
        foreach (var name in names)
        {
          // Если есть поле с организационно-правовой формой, добавить значение ОПФ к наименованию.
          if (legalForm != null && !name.Value.Contains(legalForm.Value))
            name.Value = string.Format("{0} {1}", legalForm.Value, name.Value);
          var normalizedValue = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(name.Value);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.MatchAnd, normalizedValue));
          var fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "Name", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Company.PublicConstants.BusinessUnit.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
          fieldQueries.Add(Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.MatchAnd, normalizedValue));
          fuzzyQuery = Commons.PublicFunctions.Module.CreateSearchFieldQuery(name, "ShortName", ElasticsearchTypes.FuzzyOr, normalizedValue);
          fuzzyQuery.ScoreMinLimit = Company.PublicConstants.BusinessUnit.ElasticsearchMinScore;
          fieldQueries.Add(fuzzyQuery);
        }
      }
      
      // Выполнить запрос.
      var searchQuery = ArioFactElasticsearchData.Create();
      searchQuery.EntityName = Company.BusinessUnits.Info.Name;
      searchQuery.Fact = fact;
      searchQuery.Queries = fieldQueries;
      return Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(searchQuery);
    }

    /// <summary>
    /// Поиск контактов по фактам Арио с учетом типа.
    /// </summary>
    /// <param name="arioDocument">Документ Арио.</param>
    /// <param name="factType">Тип факта.</param>
    /// <param name="filterCounterpartyId">ИД компании для фильтрации.</param>
    /// <returns>Найденный контакт.</returns>
    [Public]
    public virtual IRecognizedOfficial GetRecognizedContactFuzzy(IArioDocument arioDocument, string factType, int filterCounterpartyId)
    {
      var recognized = RecognizedOfficial.Create();
      
      // Отфильтровать по нужному типу факта, отсортировать по вероятности.
      var facts = arioDocument.Facts
        .Where(x => x.Name == ArioGrammars.LetterPersonFact.Name &&
               x.Fields.Any(f => f.Name == ArioGrammars.LetterPersonFact.TypeField && f.Value == factType) &&
               x.Fields.Any(f => f.Name == ArioGrammars.LetterPersonFact.NameField && !string.IsNullOrWhiteSpace(f.Value)))
        .OrderByDescending(x => x.Fields.First(f => f.Name == ArioGrammars.LetterPersonFact.NameField).Probability)
        .ToList();
      
      foreach (var fact in facts)
      {
        var contactNameField = fact.Fields.First(x => x.Name == ArioGrammars.LetterPersonFact.NameField);
        recognized.Contact = Sungero.Parties.PublicFunctions.Contact.GetContactsByNameFuzzy(contactNameField.Value, filterCounterpartyId);
        
        if (recognized.Contact != null)
        {
          recognized.Fact = fact;
          recognized.Probability = contactNameField.Probability;
          break;
        }
      }
      
      return recognized;
    }

    #endregion

    #endregion

    #region Поиск по штрихкодам

    /// <summary>
    /// Получить документ по штрихкоду.
    /// </summary>
    /// <param name="documentInfo">Информация о документе.</param>
    /// <returns>Документ, если он найден в системе. Иначе - null.</returns>
    [Public]
    public virtual IOfficialDocument GetDocumentByBarcode(IDocumentInfo documentInfo)
    {
      var arioDocument = documentInfo.ArioDocument;
      var document = Docflow.OfficialDocuments.Null;
      using (var body = new MemoryStream(arioDocument.BodyFromArio))
      {
        var docId = Functions.Module.SearchDocumentBarcodeIds(body).FirstOrDefault();
        // FOD на пустом List<int> вернет 0.
        if (docId != 0)
        {
          document = OfficialDocuments.GetAll().FirstOrDefault(x => x.Id == docId);
          // Если документ по штрихкоду нашелся в системе.
          if (document != null)
            documentInfo.FoundByBarcode = true;
        }
      }
      
      return document;
    }

    /// <summary>
    /// Поиск Id документа по штрихкодам.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список распознанных Id документа.</returns>
    /// <remarks>
    /// Поиск штрихкодов осуществляется только на первой странице документа.
    /// Формат штрихкода: Code128.
    /// </remarks>
    [Public]
    public virtual List<int> SearchDocumentBarcodeIds(System.IO.Stream document)
    {
      var result = new List<int>();
      try
      {
        var barcodeReader = AsposeExtensions.BarcodeReader.Create();
        var barcodeList = barcodeReader.Extract(document, Aspose.BarCode.BarCodeRecognition.DecodeType.Code128);
        if (!barcodeList.Any())
          return result;
        
        var tenantId = Docflow.PublicFunctions.Module.Remote.GetCurrentTenantId();
        var formattedTenantId = Docflow.PublicFunctions.Module.FormatTenantIdForBarcode(tenantId).Trim();
        var ourBarcodes = barcodeList.Where(b => b.Contains(formattedTenantId));
        foreach (var barcode in ourBarcodes)
        {
          int id;
          // Формат штрихкода "id тенанта - id документа".
          var stringId = barcode.Split(new string[] { " - ", "-" }, StringSplitOptions.None).Last();
          if (int.TryParse(stringId, out id))
            result.Add(id);
        }
      }
      catch (AsposeExtensions.BarcodeReaderException e)
      {
        Logger.Error(e.Message);
      }
      return result;
    }

    #endregion

    #region Создание документа из тела письма

    /// <summary>
    /// Создание документа на основе тела письма.
    /// </summary>
    /// <param name="documentPackage">Пакет документов.</param>
    [Public]
    public virtual void CreateDocumentFromEmailBody(IDocumentPackage documentPackage)
    {
      var blobPackage = documentPackage.BlobPackage;
      
      // Для писем без тела не создавать простой документ.
      if (blobPackage.MailBodyBlob != null)
      {
        try
        {
          var emailBody = this.CreateSimpleDocumentFromEmailBody(blobPackage, documentPackage.Responsible);
          var documentInfo = new DocumentInfo();
          documentInfo.Document = emailBody;
          documentInfo.IsRecognized = false;
          documentInfo.IsEmailBody = true;
          documentPackage.DocumentInfos.Add(documentInfo);
        }
        catch (Exception ex)
        {
          this.LogError("Smart processing. CreateDocumentFromEmailBody. Error while creating document from email body.", ex, blobPackage);
          documentPackage.FailedCreateDocumentFromEmailBody = true;
        }
      }
    }

    /// <summary>
    /// Создать документ из тела эл. письма.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <param name="responsible">Сотрудник, ответственный за обработку документов.</param>
    /// <returns>ИД созданного документа.</returns>
    [Public, Remote]
    public virtual ISimpleDocument CreateSimpleDocumentFromEmailBody(IBlobPackage blobPackage,
                                                                     IEmployee responsible)
    {
      var emailBodyName = Resources.EmailBodyDocumentNameFormat(blobPackage.FromAddress).ToString();
      var document = SimpleDocuments.Create();

      this.FillSimpleDocumentProperties(document, null, responsible, emailBodyName);
      this.FillDeliveryMethod(document, blobPackage.SourceType);
      this.FillVerificationState(document);
      
      // Наименование и содержание.
      if (!string.IsNullOrWhiteSpace(blobPackage.Subject))
        emailBodyName = string.Format("{0} \"{1}\"", emailBodyName, blobPackage.Subject);
      
      if (document.DocumentKind != null && document.DocumentKind.GenerateDocumentName.Value)
      {
        // Автоформируемое имя.
        document.Subject = Docflow.PublicFunctions.OfficialDocument.AddClosingQuoteToSubject(emailBodyName, document);
      }
      else
      {
        // Не автоформируемое имя.
        document.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(emailBodyName, document);
      }
      
      var mailBody = string.Empty;
      using (var reader = new StreamReader(blobPackage.MailBodyBlob.Body.Read(), System.Text.Encoding.UTF8))
        mailBody = reader.ReadToEnd();
      
      using (var body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(mailBody)))
      {
        document.CreateVersion();
        var version = document.LastVersion;
        if (System.IO.Path.GetExtension(blobPackage.MailBodyBlob.FilePath).ToLower() == Module.HtmlExtension.WithDot)
        {
          var pdfConverter = AsposeExtensions.Converter.Create();
          using (var pdfDocumentStream = pdfConverter.GeneratePdf(body, Module.HtmlExtension.WithoutDot))
          {
            if (pdfDocumentStream != null)
            {
              version.Body.Write(pdfDocumentStream);
              version.AssociatedApplication = Content.AssociatedApplications.GetByExtension(Docflow.PublicConstants.OfficialDocument.PdfExtension);
            }
          }
        }
        
        // Если тело письма не удалось преобразовать в pdf или расширение не html, то в тело пишем исходный файл.
        if (version.Body.Size == 0)
        {
          version.Body.Write(body);
          version.AssociatedApplication = Docflow.PublicFunctions.Module.GetAssociatedApplicationByFileName(blobPackage.MailBodyBlob.FilePath);
        }
      }
      
      document.Save();
      
      return document;
    }

    /// <summary>
    /// Удалить изображения из тела письма.
    /// </summary>
    /// <param name="mailBody">Содержимое тела письма.</param>
    /// <param name="filePath">Относительный путь до файла с телом письма.</param>
    /// <returns>Содержимое тела письма без ссылок на изображения.</returns>
    [Public, Obsolete("Теперь картинки корректно отображаются в теле письма, нет необходимости их удалять.")]
    public virtual string RemoveImagesFromEmailBody(string mailBody, string filePath)
    {
      // Нет смысла удалять изображения в файлах, расширение которых не html.
      if (Path.GetExtension(filePath).ToLower() == Module.HtmlExtension.WithDot)
      {
        try
        {
          mailBody = System.Text.RegularExpressions.Regex.Replace(mailBody, @"<img([^\>]*)>", string.Empty);

          // В некоторых случаях Aspose не может распознать файл как html, поэтому добавляем тег html, если его нет.
          if (!mailBody.Contains(Module.HtmlTags.MaskForSearch))
            mailBody = string.Concat(Module.HtmlTags.StartTag, mailBody, Module.HtmlTags.EndTag);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("RemoveImagesFromEmailBody: Cannot remove images from email body.", ex);
        }
      }

      return mailBody;
    }

    #endregion

    #region Сборка и связывание пакета

    /// <summary>
    /// Упорядочить и связать документы в пакете.
    /// </summary>
    /// <param name="package">Пакет документов.</param>
    [Public]
    public virtual void OrderAndLinkDocumentPackage(IDocumentPackage package)
    {
      var packageId = package.BlobPackage.PackageId;
      this.LogMessage("Smart processing. OrderAndLinkDocumentPackage...", packageId);
      
      if (!package.DocumentInfos.Any())
        return;

      try
      {
        // Получить ведущий документ из распознанных документов комплекта. Если список пуст, то из нераспознанных.
        var leadingDocument = this.GetLeadingDocument(package);
        foreach (var documentInfo in package.DocumentInfos)
          documentInfo.IsLeadingDocument = Equals(documentInfo.Document, leadingDocument);
        
        this.LinkDocuments(package);
        
        // Для документов, нераспознанных Ario, заполнить имена:
        // со сканера - шаблонным названием,
        // с электронной почты - значением исходного вложения.
        // Для распознанных документов без автоимени заполнить имя:
        // если документ с почты - первоначальным именем файла.
        this.RenameDocuments(package);
      }
      catch (Exception ex)
      {
        this.LogError("Smart processing. OrderAndLinkDocumentPackage. Error while ordering, linking and renaming documents.", ex, packageId);
        package.FailedOrderAndLinkDocuments = true;
      }
      
      this.LogMessage("Smart processing. OrderAndLinkDocumentPackage.", packageId);
      
    }

    /// <summary>
    /// Определить ведущий документ распознанного комплекта.
    /// </summary>
    /// <param name="package">Комплект документов.</param>
    /// <returns>Ведущий документ.</returns>
    [Public]
    public virtual IOfficialDocument GetLeadingDocument(IDocumentPackage package)
    {
      if (package == null)
        throw new ApplicationException(string.Format("Parameter {0} is required.", nameof(package)));
      var blobPackage = package.BlobPackage;
      var packagePriority = new Dictionary<IDocumentInfo, int>();
      var leadingDocumentPriority = Functions.Module.GetPackageDocumentTypePriorities();
      this.LogMessage(blobPackage, "Smart processing. DocumentType priorities:");
      var priorityDescriptions = leadingDocumentPriority.Select(x => string.Format("-- Priority: {0}. Type: {1}.", x.Value, x.Key));
      foreach (var description in priorityDescriptions)
        this.LogMessage(blobPackage, description);
      this.LogMessage(blobPackage, "Smart processing. Package documents priorities:");
      int priority;
      foreach (var documentInfo in package.DocumentInfos)
      {
        var typeGuid = documentInfo.Document.GetType().GetFinalType();
        leadingDocumentPriority.TryGetValue(typeGuid, out priority);
        packagePriority.Add(documentInfo, priority);
        this.LogMessage(blobPackage,
                        "-- Priority: {0}. Type: {1}. Document: (ID={2}) {3}.",
                        priority,
                        typeGuid,
                        documentInfo.Document.Id,
                        documentInfo.Document.DisplayValue);
      }
      
      var leadingDocument = packagePriority
        .OrderByDescending(p => p.Value)
        .OrderByDescending(d => d.Key.IsRecognized)
        .FirstOrDefault().Key.Document;
      this.LogMessage(blobPackage,
                      "Smart processing. Package leading document: Document: (ID={0}) {1}.",
                      leadingDocument.Id,
                      leadingDocument.DisplayValue);
      return leadingDocument;
    }

    /// <summary>
    /// Связать документы комплекта.
    /// </summary>
    /// <param name="package">Распознанные документы комплекта.</param>
    /// <remarks>
    /// Для распознанных документов комплекта, если ведущий документ - простой, то тип связи - "Прочие". Иначе "Приложение".
    /// Для нераспознанных документов комплекта - тип связи "Прочие".
    /// </remarks>
    [Public]
    public virtual void LinkDocuments(IDocumentPackage package)
    {
      var leadingDocument = package.DocumentInfos.Where(i => i.IsLeadingDocument).Select(d => d.Document).FirstOrDefault();
      var leadingDocumentIsSimple = SimpleDocuments.Is(leadingDocument);
      
      var relation = leadingDocumentIsSimple
        ? Docflow.PublicConstants.Module.SimpleRelationName
        : Docflow.PublicConstants.Module.AddendumRelationName;
      
      // Связать приложения с ведущим документом.
      var addenda = package.DocumentInfos
        .Where(i => !i.IsLeadingDocument && i.ArioDocument != null && i.ArioDocument.IsProcessedByArio)
        .Select(d => d.Document);
      foreach (var addendum in addenda)
      {
        addendum.Relations.AddFrom(relation, leadingDocument);
        addendum.Save();
      }
      
      // Связать документы, которые не отправлялись в Арио, и тело письма с ведущим документом, тип связи - "Прочие".
      var notRecognizedDocuments = package.DocumentInfos
        .Where(i => !i.IsLeadingDocument && (i.ArioDocument == null || !i.ArioDocument.IsProcessedByArio))
        .Select(d => d.Document);
      foreach (var notRecognizedDocument in notRecognizedDocuments)
      {
        notRecognizedDocument.Relations.AddFrom(Docflow.PublicConstants.Module.SimpleRelationName, leadingDocument);
        notRecognizedDocument.Save();
      }
    }

    /// <summary>
    /// Переименовать документы в комплекте.
    /// </summary>
    /// <param name="package">Комплект документов.</param>
    /// <remarks>
    /// Для почты: всем документам без автоимени присвоить оригинальное имя файла,
    /// простому документу с автоименем - положить имя файла в содержание.
    /// Для папки: если неклассифицированных документов несколько и ведущий документ простой,
    /// то у ведущего будет номер 1, у остальных - следующие по порядку.
    /// </remarks>
    [Public]
    public virtual void RenameDocuments(IDocumentPackage package)
    {
      // Поступление документов с эл. почты.
      if (package.BlobPackage.SourceType == SmartProcessing.BlobPackage.SourceType.Mail)
      {
        // Переименовать простые документы. Не переименовывать, если документ найден по штрихкоду.
        var simpleDocumentInfos = package.DocumentInfos.Where(i => SimpleDocuments.Is(i.Document) &&
                                                              !i.IsEmailBody &&
                                                              !i.FoundByBarcode);
        foreach (var simpleDocumentInfo in simpleDocumentInfos)
        {
          var originalFileName = simpleDocumentInfo.ArioDocument.OriginalBlob.OriginalFileName;
          if (!string.IsNullOrWhiteSpace(originalFileName))
          {
            var document = simpleDocumentInfo.Document;
            if (document.DocumentKind.GenerateDocumentName == true)
              document.Subject = originalFileName;
            else
              document.Name = originalFileName;
            document.Save();
          }
        }
        
        // Переименовать распознанные документы. Не переименовывать, если документ найден по штрихкоду.
        var recognizedDocumentInfos = package.DocumentInfos.Where(i => !SimpleDocuments.Is(i.Document) &&
                                                                  !i.IsEmailBody &&
                                                                  !i.FoundByBarcode);
        foreach (var recognizedDocumentInfo in recognizedDocumentInfos)
        {
          var originalFileName = recognizedDocumentInfo.ArioDocument.OriginalBlob.OriginalFileName;
          if (!string.IsNullOrWhiteSpace(originalFileName))
          {
            var document = recognizedDocumentInfo.Document;
            if (document.DocumentKind.GenerateDocumentName != true)
              document.Name = originalFileName;
            document.Save();
          }
        }
        return;
      }
      
      // Поступление документов с папки.
      if (package.DocumentInfos.Select(d => SimpleDocuments.Is(d.Document)).Count() < 2)
        return;
      
      // Если ведущий документ SimpleDocument, то переименовываем его,
      // для того чтобы в имени содержался его порядковый номер.
      int simpleDocumentNumber = 1;
      var leadingDocument = package.DocumentInfos
        .Where(i => i.IsLeadingDocument && !i.FoundByBarcode)
        .Select(d => d.Document)
        .FirstOrDefault();
      var leadingDocumentIsSimple = SimpleDocuments.Is(leadingDocument);
      if (leadingDocumentIsSimple)
      {
        leadingDocument.Name = Resources.DocumentNameFormat(simpleDocumentNumber);
        leadingDocument.Save();
        simpleDocumentNumber++;
      }
      
      var addenda = package.DocumentInfos.Where(i => !i.IsLeadingDocument && !i.FoundByBarcode).Select(d => d.Document);
      foreach (var addendum in addenda)
      {
        if (SimpleDocuments.Is(addendum))
        {
          addendum.Name = leadingDocumentIsSimple
            ? Resources.DocumentNameFormat(simpleDocumentNumber)
            : Resources.AttachmentNameFormat(simpleDocumentNumber);
          addendum.Save();
          simpleDocumentNumber++;
        }
      }
    }

    #endregion

    #region Отправка в работу

    /// <summary>
    /// Отправить документы ответственному.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    [Public]
    public virtual void SendToResponsible(IDocumentPackage documentPackage)
    {
      var packageId = documentPackage.BlobPackage.PackageId;
      this.LogMessage("Smart processing. SendToResponsible...", packageId);
      
      var responsible = documentPackage.Responsible;
      
      // Собрать пакет документов. Порядок важен, чтобы ведущий был первым.
      var leadingDocument = documentPackage.DocumentInfos.Where(i => i.IsLeadingDocument).Select(d => d.Document).FirstOrDefault();
      // Если при определении ведущего документа возникли ошибки, то берем первый попавшийся документ.
      if (leadingDocument == null)
        leadingDocument = documentPackage.DocumentInfos.Select(d => d.Document).FirstOrDefault();
      var package = documentPackage.DocumentInfos.OrderByDescending(d => d.IsLeadingDocument).Select(d => d.Document).ToList();
      if (!package.Any())
        return;
      
      // Тема.
      var task = VerificationTasks.Create();
      task.Subject = package.Count() > 1
        ? Resources.CheckPackageTaskNameFormat(leadingDocument)
        : Resources.CheckDocumentTaskNameFormat(leadingDocument);
      if (task.Subject.Length > task.Info.Properties.Subject.Length)
        task.Subject = task.Subject.Substring(0, task.Info.Properties.Subject.Length);
      
      // Записать наименование ведущего документа в свойство задачи для формирования темы задания.
      task.LeadingDocumentName = leadingDocument.ToString();
      
      // Вложить в задачу и выдать права на документы ответственному.
      foreach (var document in package)
      {
        try
        {
          document.AccessRights.Grant(responsible, DefaultAccessRightsTypes.FullAccess);
          document.AccessRights.Save();
        }
        catch (Exception e)
        {
          Logger.DebugFormat("Cannot grant rights to responsible: {0}", e.Message);
        }
        
        task.Attachments.Add(document);
      }
      
      // Добавить наблюдателями ответственных за документы, которые вернулись по ШК.
      var foundByBarcodeDocuments = documentPackage.DocumentInfos.Where(x => x.FoundByBarcode).Select(x => x.Document).ToList();
      var responsibleEmployees = this.GetDocumentsResponsibleEmployees(foundByBarcodeDocuments);
      responsibleEmployees = responsibleEmployees.Where(r => !Equals(r, responsible)).ToList();
      foreach (var responsibleEmployee in responsibleEmployees)
      {
        var observer = task.Observers.AddNew();
        observer.Observer = responsibleEmployee;
      }
      
      task.Assignee = responsible;
      task.ActiveText = this.GetVerificationTaskText(documentPackage);
      
      task.NeedsReview = false;
      task.Deadline = Calendar.Now.AddWorkingHours(task.Assignee, 4);
      task.Save();
      task.Start();
      
      Logger.Debug("Задача на верификацию отправлена в работу");
      
      // Старт фонового процесса для удаления блобов.
      Jobs.DeleteBlobPackages.Enqueue();
      
      this.LogMessage("Smart processing. SendToResponsible.", packageId);
    }

    /// <summary>
    /// Запустить асинхронные обработчики выдачи прав.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    public virtual void EnqueueGrantAccessRightsJobs(IDocumentPackage documentPackage)
    {
      var packageId = documentPackage.BlobPackage.PackageId;
      this.LogMessage("Smart processing. EnqueueGrantAccessRightsJobs started.", packageId);
      var documents = documentPackage.DocumentInfos.Select(d => d.Document).ToList<IOfficialDocument>();
      if (!documents.Any())
        return;
      
      var enqueueGrantAccessRightsToProjectDocumentJob = false;
      foreach (var document in documents)
      {
        Docflow.PublicFunctions.Module.CreateGrantAccessRightsToDocumentAsyncHandler(document.Id, new List<int>(), true);
        this.LogMessage(string.Format("Smart processing. GrantAccessRightsToDocumentAsync started. Document ID = ({0})", document.Id), packageId);
        
        var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)document).Params;
        if (documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument))
        {
          enqueueGrantAccessRightsToProjectDocumentJob = true;
          documentParams.Remove(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument);
        }
      }
      
      if (enqueueGrantAccessRightsToProjectDocumentJob)
      {
        Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
        this.LogMessage("Smart processing. GrantAccessRightsToProjectDocuments job started.", packageId);
      }
      
      this.LogMessage("Smart processing. EnqueueGrantAccessRightsJobs completed.", packageId);
    }
    
    /// <summary>
    /// Получить список ответственных за документы.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список ответственных.</returns>
    /// <remarks>Ответственных искать только у документов, тип которых: договорной документ, акт, накладная, УПД.</remarks>
    [Public]
    public virtual List<IEmployee> GetDocumentsResponsibleEmployees(List<IOfficialDocument> documents)
    {
      var responsibleEmployees = new List<IEmployee>();
      var withResponsibleDocuments = documents.Where(d => Contracts.ContractualDocuments.Is(d) ||
                                                     FinancialArchive.ContractStatements.Is(d) ||
                                                     FinancialArchive.Waybills.Is(d) ||
                                                     FinancialArchive.UniversalTransferDocuments.Is(d));
      foreach (var document in withResponsibleDocuments)
      {
        var responsibleEmployee = Employees.Null;
        responsibleEmployee = Docflow.PublicFunctions.OfficialDocument.GetDocumentResponsibleEmployee(document);
        
        if (responsibleEmployee != Employees.Null && responsibleEmployee.IsSystem != true)
          responsibleEmployees.Add(responsibleEmployee);
      }
      
      return responsibleEmployees.Distinct().ToList();
    }

    /// <summary>
    /// Получить текст задачи на проверку документов.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Текст задачи на проверку документов.</returns>
    [Public]
    public virtual string GetVerificationTaskText(IDocumentPackage documentPackage)
    {
      var taskActiveText = new List<string>();
      
      // Добавить в текст задачи список документов, которые занесены по штрихкоду.
      taskActiveText.Add(this.GetFoundByBarcodeDocumentsTaskText(documentPackage));
      
      // Добавить в текст задачи список неклассифицированных документов.
      taskActiveText.Add(this.GetNotClassifiedDocumentsTaskText(documentPackage));
      
      // Добавить в текст задачи список документов, которые не удалось зарегистрировать.
      taskActiveText.Add(this.GetFailedRegistrationDocumentsTaskText(documentPackage));
      
      // Добавить в текст задачи список документов, которые были заблокированы при занесении новой версии.
      taskActiveText.Add(this.GetLockedDocumentsTaskText(documentPackage));
      
      // Добавить в текст задачи список ошибок при обработке пакета документов.
      taskActiveText.Add(this.GetDocumentPackageErrorsTaskText(documentPackage));
      
      taskActiveText = taskActiveText.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
      
      if (taskActiveText.Count > 0)
        taskActiveText.Add(Resources.ContactSysAdminIfNeeded);
      
      return string.Join(Environment.NewLine + Environment.NewLine, taskActiveText);
    }

    /// <summary>
    /// Получить блок текста задачи с ошибками обработки пакета документов.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Блок текста задачи с ошибками обработки пакета документов.</returns>
    public virtual string GetDocumentPackageErrorsTaskText(IDocumentPackage documentPackage)
    {
      var errorsTaskText = new List<string>();
      
      errorsTaskText.Add(Resources.DocumentPackageHasErrors);
      
      // Блок ошибок на уровне отдельных документов.
      foreach (var documentInfo in documentPackage.DocumentInfos)
      {
        if (documentInfo.FailedCreateDocument || documentInfo.FailedArioProcessDocument)
          errorsTaskText.Add(string.Format("    {0}: {1}{2}{3}",
                                           Hyperlinks.Get(documentInfo.Document),
                                           documentInfo.FailedCreateDocument ? Resources.FailedCreateDocumentTaskTextFormat(documentInfo.ArioDocument.RecognitionInfo.RecognizedClass) : string.Empty,
                                           documentInfo.FailedCreateDocument && documentInfo.FailedArioProcessDocument ? ", " : string.Empty,
                                           documentInfo.FailedArioProcessDocument ? Resources.FailedArioProcessDocumentTaskText : string.Empty));
      }
      
      // Блок ошибок на уровне пакета.
      if (documentPackage.FailedCreateDocumentFromEmailBody)
        errorsTaskText.Add(Environment.NewLine +
                           (documentPackage.FailedCreateDocumentFromEmailBody ? Resources.CreateDocumentFromEmailBodyErrorsTaskTextFormat(documentPackage.BlobPackage.FromAddress,
                                                                                                                                          documentPackage.BlobPackage.Subject) : string.Empty));
      if (documentPackage.FailedOrderAndLinkDocuments)
        errorsTaskText.Add(Environment.NewLine +
                           (documentPackage.FailedCreateDocumentFromEmailBody ? Resources.OrderAndLinkErrorsTaskText : string.Empty));
      
      if (errorsTaskText.Count > 1)
        return string.Join(Environment.NewLine, errorsTaskText);
      else
        return string.Empty;
    }

    /// <summary>
    /// Получить блок текста задачи со списком документов, которые не удалось классифицировать.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Блок текста задачи со списком гиперссылок на документы, которые не удалось классифицировать.</returns>
    [Public]
    public virtual string GetNotClassifiedDocumentsTaskText(IDocumentPackage documentPackage)
    {
      // Считать неклассифицированными все простые документы и те, чей класс по Арио не имеет соответствия в системе.
      // Не нужно считать неклассифицированными документами тело письма и документ, найденный по штрихкоду.
      // Также не считаются неклассифицированными документы, которые не удалось создать в системе.
      var notClassifiedDocuments = documentPackage.DocumentInfos
        .Where(i => !i.FailedCreateDocument && !i.FailedArioProcessDocument)
        .Where(i => !i.IsRecognized && !i.IsEmailBody && !i.FoundByBarcode || i.IsRecognized && SimpleDocuments.Is(i.Document))
        .Select(d => d.Document)
        .ToList();
      
      if (notClassifiedDocuments.Any())
      {
        var failedClassifyTaskText = notClassifiedDocuments.Count() == 1
          ? Resources.FailedClassifyDocumentTaskText
          : Resources.FailedClassifyDocumentsTaskText;
        
        return this.FormDocumentsTaskTextWithHyperlinks(failedClassifyTaskText, notClassifiedDocuments);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Получить блок текста задачи со списком документов, которые не удалось зарегистрировать.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Блок текста задачи со списком гиперссылок на документы, которые не удалось зарегистрировать.</returns>
    [Public]
    public virtual string GetFailedRegistrationDocumentsTaskText(IDocumentPackage documentPackage)
    {
      // Собрать документы, которые не удалось зарегистрировать.
      var failedRegistrationDocuments = documentPackage.DocumentInfos
        .Where(i => i.RegistrationFailed)
        .Select(d => d.Document)
        .ToList();
      
      if (failedRegistrationDocuments.Any())
      {
        failedRegistrationDocuments = failedRegistrationDocuments.OrderBy(x => x.DocumentKind.Name).ToList();
        var documentsText = failedRegistrationDocuments.Count() == 1 ? Resources.Document : Resources.Documents;
        var documentKinds = failedRegistrationDocuments.Select(x => string.Format("\"{0}\"", x.DocumentKind.Name)).Distinct();
        var documentKindsText = documentKinds.Count() == 1 ? Resources.Kind : Resources.Kinds;
        var documentKindsListText = string.Join(", ", documentKinds);
        
        var failedRegistrationDocumentsTaskText = string.Format(Resources.DocumentsWithRegistrationFailureTaskText,
                                                                documentsText, documentKindsText, documentKindsListText);
        
        return this.FormDocumentsTaskTextWithHyperlinks(failedRegistrationDocumentsTaskText, failedRegistrationDocuments);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Получить блок текста задачи со списком документов, которые занесены по штрихкоду.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Блок текста задачи со списком гиперссылок на документы, которые занесены по штрихкоду.</returns>
    [Public]
    public virtual string GetFoundByBarcodeDocumentsTaskText(IDocumentPackage documentPackage)
    {
      // Сформировать список документов, которые занесены по штрихкоду.
      var foundByBarcodeDocuments = documentPackage.DocumentInfos
        .Where(i => i.FoundByBarcode && !i.FailedCreateVersion)
        .Select(d => d.Document)
        .ToList();
      
      if (foundByBarcodeDocuments.Any())
      {
        var documentsFoundBarcodeTaskText = foundByBarcodeDocuments.Count() == 1
          ? Resources.DocumentFoundByBarcodeTaskText
          : Resources.DocumentsFoundByBarcodeTaskText;
        
        return this.FormDocumentsTaskTextWithHyperlinks(documentsFoundBarcodeTaskText, foundByBarcodeDocuments);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Получить блок текста задачи со списком документов, которые были заблокированы при занесении новой версии.
    /// </summary>
    /// <param name="documentPackage">Пакет документов в системе.</param>
    /// <returns>Блок текста задачи со списком гиперссылок на документы, которые были заблокированы при занесении новой версии.</returns>
    [Public]
    public virtual string GetLockedDocumentsTaskText(IDocumentPackage documentPackage)
    {
      // Сформировать список заблокированных документов.
      var lockedDocuments = documentPackage.DocumentInfos
        .Where(i => i.FailedCreateVersion)
        .Select(d => d.Document)
        .ToList();
      
      if (lockedDocuments.Any())
      {
        var failedCreateVersionTaskText = lockedDocuments.Count() == 1
          ? Resources.FailedCreateVersionTaskText
          : Resources.FailedCreateVersionsTaskText;

        var lockedDocumentsHyperlinksLabels = new List<string>();
        foreach (var lockedDocument in lockedDocuments)
        {
          var loginId = Locks.GetLockInfo(lockedDocument).LoginId;
          var employee = Employees.GetAll(x => x.Login.Id == loginId).FirstOrDefault();
          // Текстовка на случай, когда блокировка снята в момент создания задачи.
          var employeeLabel = Resources.DocumentWasLockedTaskText.ToString();
          if (employee != null)
            employeeLabel = string.Format(Resources.DocumentLockedByEmployeeTaskText,
                                          Hyperlinks.Get(employee));
          var documentHyperlink = Hyperlinks.Get(lockedDocument);
          lockedDocumentsHyperlinksLabels.Add(string.Format("{0} {1}", documentHyperlink, employeeLabel));
        }
        
        return this.FormDocumentsTaskTextWithHyperlinks(failedCreateVersionTaskText, lockedDocumentsHyperlinksLabels);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Сформировать блок текста задачи на верификацию с гиперссылками на документы комплекта.
    /// </summary>
    /// <param name="documentsSectionTitle">Заголовок для гиперссылок на документы комплекта.</param>
    /// <param name="documentsForHyperlinks">Документы, на которые в текст будут вставлены гиперссылки.</param>
    /// <returns>Текст с гиперссылками на документы комплекта.</returns>
    [Public]
    public virtual string FormDocumentsTaskTextWithHyperlinks(string documentsSectionTitle,
                                                              List<IOfficialDocument> documentsForHyperlinks)
    {
      // Собрать ссылки на документы.
      var hyperlinksLabels = documentsForHyperlinks.Select(x => Hyperlinks.Get(x)).ToList();
      return this.FormDocumentsTaskTextWithHyperlinks(documentsSectionTitle, hyperlinksLabels);
    }

    /// <summary>
    /// Сформировать блок текста задачи на верификацию с гиперссылками на документы комплекта.
    /// </summary>
    /// <param name="documentsSectionTitle">Заголовок для гиперссылок на документы комплекта.</param>
    /// <param name="hyperlinksLabels">Гиперссылки.</param>
    /// <returns>Текст с гиперссылками на документы комплекта.</returns>
    [Public]
    public virtual string FormDocumentsTaskTextWithHyperlinks(string documentsSectionTitle,
                                                              List<string> hyperlinksLabels)
    {
      // Между блоками отступ 1 строка, каждая гиперссылка с новой строки с отступом 4 пробела от начала.
      var documentHyperlinksLabel = string.Join(Environment.NewLine + "    ", hyperlinksLabels);
      var documentsTaskText = string.Format("{0}{1}    {2}", documentsSectionTitle, Environment.NewLine, documentHyperlinksLabel);
      
      return documentsTaskText;
    }

    #endregion

    #region Завершение процесса обработки

    /// <summary>
    /// Завершить процесс обработки.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    [Public]
    public virtual void FinalizeProcessing(IBlobPackage blobPackage)
    {
      var packageId = blobPackage.PackageId;
      this.LogMessage("Smart processing. FinalizeProcessing...", packageId);
      
      blobPackage.ProcessState = SmartProcessing.BlobPackage.ProcessState.Processed;
      blobPackage.Save();
      
      this.LogMessage("Smart processing. FinalizeProcessing.", packageId);
    }

    #endregion

    #region Тесты

    /// <summary>
    /// Обработать пакет бинарных образов документов (для теста).
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    [Public]
    public virtual void ProcessCapturedPackageTest(IBlobPackage blobPackage)
    {
      var arioPackage = this.UnpackArioPackage(blobPackage);
      
      var documentPackage = this.BuildDocumentPackageTest(blobPackage, arioPackage);
      
      this.OrderAndLinkDocumentPackage(documentPackage);
      
      this.SendToResponsible(documentPackage);
      
      // Вызываем асинхронную выдачу прав, так как убрали ее при сохранении.
      this.EnqueueGrantAccessRightsJobs(documentPackage);
      
      this.FinalizeProcessing(blobPackage);
    }

    /// <summary>
    /// Сформировать пакет документов (для теста).
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <param name="arioPackage">Пакет результатов обработки документов в Ario.</param>
    /// <returns>Пакет созданных документов.</returns>
    public virtual IDocumentPackage BuildDocumentPackageTest(IBlobPackage blobPackage, IArioPackage arioPackage)
    {
      var documentPackage = this.PrepareDocumentPackageTest(blobPackage, arioPackage);
      
      documentPackage.Responsible = this.GetResponsible(blobPackage);

      foreach (var documentInfo in documentPackage.DocumentInfos)
      {
        var document = this.CreateDocument(documentInfo, documentPackage);

        this.CreateVersion(document, documentInfo.ArioDocument);

        this.FillDeliveryMethod(document, blobPackage.SourceType);

        this.FillVerificationState(document);

        this.SaveDocument(document, documentInfo);
      }

      this.CreateDocumentFromEmailBody(documentPackage);

      return documentPackage;
    }

    /// <summary>
    /// Создать незаполненный пакет документов (для теста).
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <param name="arioPackage">Пакет результатов обработки документов в Ario.</param>
    /// <returns>Заготовка пакета документов.</returns>
    [Public]
    public virtual IDocumentPackage PrepareDocumentPackageTest(IBlobPackage blobPackage, IArioPackage arioPackage)
    {
      var documentPackage = Structures.Module.DocumentPackage.Create();
      
      var documentInfos = new List<IDocumentInfo>();
      foreach (var arioDocument in arioPackage.Documents)
      {
        var filePath = arioDocument.OriginalBlob.FilePath;
        arioDocument.BodyFromArio = File.ReadAllBytes(filePath);

        var documentInfo = new DocumentInfo();
        documentInfo.ArioDocument = arioDocument;
        documentInfo.IsRecognized = arioDocument.IsRecognized;
        documentInfos.Add(documentInfo);
      }

      documentPackage.DocumentInfos = documentInfos;
      documentPackage.BlobPackage = blobPackage;

      return documentPackage;
    }

    /// <summary>
    /// Запустить фоновый процесс, удаляющий пакеты бинарных образов документов, которые отправлены на верификацию.
    /// </summary>
    [Public, Remote]
    public static void RequeueDeleteBlobPackagesJob()
    {
      Jobs.DeleteBlobPackages.Enqueue();
    }

    #endregion

    #region Логирование

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="package">Пакет бинарных образов документов.</param>
    /// <param name="format">Формат сообщения.</param>
    /// <param name="args">Аргументы.</param>
    public void LogMessage(IBlobPackage package, string format, params object[] args)
    {
      this.LogMessage(string.Format(format, args),
                      package != null ? package.PackageId : string.Empty);
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="package">Пакет бинарных образов документов.</param>
    public void LogMessage(string message, IBlobPackage package)
    {
      this.LogMessage(message, package != null ? package.PackageId : string.Empty);
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="packageId">ИД пакета бинарных образов документов.</param>
    public void LogMessage(string message, string packageId)
    {
      var logMessageFormat = "{0} (pkg={1})";
      Logger.DebugFormat(logMessageFormat, message, packageId);
    }

    /// <summary>
    /// Записать ошибку в лог.
    /// </summary>
    /// <param name="message">Текст ошибки.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="package">Пакет бинарных образов документов.</param>
    public void LogError(string message, System.Exception exception, IBlobPackage package)
    {
      this.LogError(message,
                    exception,
                    package != null ? package.PackageId : string.Empty);
    }

    /// <summary>
    /// Записать ошибку в лог.
    /// </summary>
    /// <param name="message">Текст ошибки.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="packageId">ИД пакета бинарных образов документов.</param>
    public void LogError(string message, System.Exception exception, string packageId)
    {
      var logMessage = string.Format("{0} (pkg={1})", message, packageId);
      Logger.Error(logMessage, exception);
    }

    #endregion
  }
}
