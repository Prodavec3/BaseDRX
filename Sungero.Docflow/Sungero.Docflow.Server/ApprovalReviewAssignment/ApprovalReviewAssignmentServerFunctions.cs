using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalReviewAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    #endregion

    #region Лист согласования

    /// <summary>
    /// Получить модель контрола состояния листа согласования.
    /// </summary>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote]
    public StateView GetApprovalListState()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return CreateApprovalListStateView(document);
    }
    
    /// <summary>
    /// Создать модель контрола состояния листа согласования.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote(IsPure = true)]
    public static StateView CreateApprovalListStateView(IOfficialDocument document)
    {
      // Задать текст по умолчанию.
      var stateView = StateView.Create();
      stateView.AddDefaultLabel(OfficialDocuments.Resources.DocumentIsNotSigned);
      
      if (document == null)
        return stateView;
      
      // Сформировать список подписей.
      var filteredSignatures = new List<Structures.ApprovalReviewAssignment.SignaturesInfo>();
      var signatures = new List<Structures.ApprovalReviewAssignment.DocumentSignature>();
      var signatureList = new List<Domain.Shared.ISignature>();
      var externalSignatures = new List<Structures.ApprovalReviewAssignment.DocumentSignature>();
      var externalSignatureList = new List<Domain.Shared.ISignature>();
      foreach (var version in document.Versions.OrderByDescending(v => v.Created))
      {
        // Получить к версии Согласующие и Утверждающие подписи в порядке подписывания.
        var versionSignatures = Signatures.Get(version).Where(s => s.SignatureType != SignatureType.NotEndorsing).OrderByDescending(s => s.SigningDate);
        
        // Вывести информацию о подписях.
        foreach (var signature in versionSignatures)
        {
          if (signature.IsExternal == true)
          {
            externalSignatures.Add(Structures.ApprovalReviewAssignment.DocumentSignature.Create(signature.Id, signature.SigningDate, version.Number));
            externalSignatureList.Add(signature);
            continue;
          }
          var signatureTypeString = signature.SignatureType == SignatureType.Approval ?
            Constants.ApprovalReviewAssignment.ApprovalSignatureType :
            Constants.ApprovalReviewAssignment.EndorsingSignatureType;
          
          if (!filteredSignatures.Where(f => Equals(f.Signatory, signature.Signatory) && Equals(f.SubstitutedUser, signature.SubstitutedUser) && Equals(f.SignatoryType, signatureTypeString)).Any())
          {
            filteredSignatures.Add(Structures.ApprovalReviewAssignment.SignaturesInfo.Create(signature.Signatory, signature.SubstitutedUser, signatureTypeString));
            signatures.Add(Structures.ApprovalReviewAssignment.DocumentSignature.Create(signature.Id, signature.SigningDate, version.Number));
            signatureList.Add(signature);
          }
        }
      }
      
      // Проверить, что подписи есть.
      if (!signatures.Any())
        return stateView;
      
      // Добавить подписи: по убыванию даты подписи, без учета версии.
      foreach (var signatureInfo in signatures.OrderBy(s => s.SigningDate))
      {
        var signingBlock = stateView.AddBlock();
        if (externalSignatures.Any() &&
            signatureInfo.Equals(signatures.OrderBy(s => s.SigningDate).Last()))
          signingBlock.DockType = DockType.None;
        else
          signingBlock.DockType = DockType.Bottom;
        var signature = signatureList.Single(s => s.Id == signatureInfo.SignatureId);
        var versionNumber = signatureInfo.VersionNumber;
        AddSignatureInfoToBlock(signingBlock, signature, versionNumber);
      }
      
      if (externalSignatures.Any())
      {
        // Добавить информацию о внешних подписях.
        foreach (var signatureInfo in externalSignatures.OrderBy(s => s.SigningDate))
        {
          var signingBlock = stateView.AddBlock();
          var signature = externalSignatureList.Single(s => s.Id == signatureInfo.SignatureId);
          var versionNumber = signatureInfo.VersionNumber;
          AddExternalSignatureInfoToBlock(signingBlock, signature, versionNumber, document);
          signingBlock.DockType = DockType.Bottom;
        }
      }
      
      return stateView;
    }
    
    private static string GetOriginalExchangeServiceNameByDocument(IOfficialDocument document)
    {
      var task = Exchange.ExchangeDocumentProcessingTasks.GetAll().FirstOrDefault(t => t.AttachmentDetails.Any(a => a.AttachmentId == document.Id));
      
      if (task == null)
        return string.Empty;
      
      return task.ExchangeService.Name;
    }
    
    private static void AddExternalSignatureInfoToBlock(StateBlock signingBlock, Sungero.Domain.Shared.ISignature signature, int? versionNumber, IOfficialDocument document)
    {
      var certificateInfo = Sungero.Docflow.Functions.Module.GetSignatureCertificateInfo(signature.GetDataSignature());
      var parsedSubject = Sungero.Docflow.Functions.Module.ParseCertificateSubject(certificateInfo.SubjectInfo);
      
      signingBlock.AddLabel(ApprovalReviewAssignments.Resources.SignatureBlockFormat(parsedSubject.JobTitle,
                                                                                     Docflow.Server.ModuleFunctions.GetCertificateOwnerShortName(parsedSubject),
                                                                                     parsedSubject.OrganizationName,
                                                                                     parsedSubject.TIN),
                            Functions.Module.CreateHeaderStyle());
      signingBlock.AddLineBreak();
      signingBlock.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
      signingBlock.AddLineBreak();
      
      var exchangeServiceName = GetOriginalExchangeServiceNameByDocument(document);
      
      signingBlock.AddLabel(ApprovalReviewAssignments.Resources.DocumentSignedByCounterpartyIsExchangeServiceNoteFormat(exchangeServiceName),
                            Docflow.PublicFunctions.Module.CreateNoteStyle());
      
      var number = versionNumber.HasValue ? versionNumber.Value.ToString() : string.Empty;
      var numberLabel = string.Format("{0} {1}", ApprovalReviewAssignments.Resources.StateViewVersion, number);
      Functions.Module.AddInfoToRightContent(signingBlock, numberLabel, Docflow.PublicFunctions.Module.CreateNoteStyle());
      
      // Добавить информацию о валидности подписи.
      AddValidationInfoToBlock(signingBlock, signature);
      
      // Установить иконку.
      signingBlock.AssignIcon(Docflow.ApprovalReviewAssignments.Resources.ExternalSignatureIcon, StateBlockIconSize.Small);
      
      signingBlock.DockType = DockType.Bottom;
    }
    
    private static void AddSignatureInfoToBlock(StateBlock signingBlock, Sungero.Domain.Shared.ISignature signature, int? versionNumber)
    {
      // Добавить подписавшего.
      var signatory = Sungero.Company.Employees.As(signature.Signatory);
      if (signatory != null)
        signingBlock.AddLabel(string.Format("{0} {1}", signatory.JobTitle, Company.PublicFunctions.Employee.GetShortName(signatory, false)), Functions.Module.CreateHeaderStyle());
      else
        signingBlock.AddLabel(signature.Signatory.Name, Functions.Module.CreateHeaderStyle());
      
      // Добавить дату подписания.
      var signDate = signature.SigningDate.FromUtcTime().ToUserTime();
      signingBlock.AddLabel(string.Format("{0}: {1}",
                                          OfficialDocuments.Resources.StateViewDate.ToString(),
                                          Functions.Module.ToShortDateShortTime(signDate)),
                            Functions.Module.CreatePerformerDeadlineStyle());
      
      // Добавить замещаемого.
      var substitutedUser = GetSubstitutedEmployee(signature);
      if (!string.IsNullOrEmpty(substitutedUser))
      {
        signingBlock.AddLineBreak();
        signingBlock.AddLabel(substitutedUser);
      }
      
      // Добавить комментарий пользователя.
      var comment = GetSignatureComment(signature);
      if (!string.IsNullOrEmpty(comment))
      {
        signingBlock.AddLineBreak();
        signingBlock.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
        signingBlock.AddLineBreak();
        
        var commentStyle = Functions.Module.CreateNoteStyle();
        signingBlock.AddLabel(comment, commentStyle);
      }
      
      // Добавить номер версии документа.
      var number = versionNumber.HasValue ? versionNumber.Value.ToString() : string.Empty;
      var numberLabel = string.Format("{0} {1}", ApprovalReviewAssignments.Resources.StateViewVersion, number);
      Functions.Module.AddInfoToRightContent(signingBlock, numberLabel, Docflow.PublicFunctions.Module.CreateNoteStyle());
      
      // Добавить информацию о валидности подписи.
      AddValidationInfoToBlock(signingBlock, signature);
      
      // Установить иконку.
      SetIconToBlock(signingBlock, signature);
    }
    
    private static void AddValidationInfoToBlock(StateBlock block, Sungero.Domain.Shared.ISignature signature)
    {
      var redTextStyle = Functions.Module.CreateStyle(Sungero.Core.Colors.Common.Red);
      var greenTextStyle = Functions.Module.CreateStyle(Sungero.Core.Colors.Common.Green);
      var separator = ". ";
      
      var errorValidationText = string.Empty;
      var validationInfo = GetValidationInfo(signature);
      if (validationInfo.IsInvalidCertificate)
        errorValidationText += ApprovalReviewAssignments.Resources.StateViewCertificateIsNotValid + separator;
      
      if (validationInfo.IsInvalidData)
        errorValidationText += ApprovalReviewAssignments.Resources.StateViewDocumentIsChanged + separator;
      
      if (validationInfo.IsInvalidAttributes)
        errorValidationText += ApprovalReviewAssignments.Resources.StateViewSignatureAttributesNotValid + separator;
      
      // Если нет ошибок - выйти.
      if (string.IsNullOrWhiteSpace(errorValidationText))
        return;
      
      block.AddLineBreak();
      block.AddLabel(errorValidationText, redTextStyle);
      
      // Для подписи с невалидными атрибутами добавить информацию, что тело не было изменено.
      if (validationInfo.IsInvalidAttributes && !validationInfo.IsInvalidData)
      {
        var trueValidationText = ApprovalReviewAssignments.Resources.StateViewDocumentIsValid + separator;
        block.AddLabel(trueValidationText, greenTextStyle);
      }
    }
    
    private static string GetSubstitutedEmployee(Sungero.Domain.Shared.ISignature signature)
    {
      var substituted = Sungero.Company.Employees.As(signature.SubstitutedUser);
      
      if (substituted == null)
        return string.Empty;
      
      var jobTitle = Company.PublicFunctions.Employee.GetJobTitle(substituted, DeclensionCase.Accusative);
      
      // TODO: 35010.
      if (System.Threading.Thread.CurrentThread.CurrentUICulture.Equals(System.Globalization.CultureInfo.CreateSpecificCulture("ru-RU")))
        jobTitle = jobTitle.ToLower();

      jobTitle = string.IsNullOrEmpty(jobTitle) ? jobTitle : string.Format("{0} ", jobTitle);
      
      return string.Format("{0} {1}{2}",
                           ApprovalReviewAssignments.Resources.StateViewSubstitutedUser,
                           jobTitle,
                           Company.PublicFunctions.Employee.GetShortName(substituted, DeclensionCase.Accusative, false));
    }
    
    private static string GetSignatureComment(Sungero.Domain.Shared.ISignature signature)
    {
      if (string.IsNullOrEmpty(signature.Comment))
        return string.Empty;
      
      var comment = signature.Comment;
      
      // Взять первые 2 строки комментария.
      // TODO Убрать укорачивание комментария после исправления 33192, 33195, 33196. Или укорачивать по-другому.
      var linesCount = comment.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
      if (linesCount > 2)
      {
        var strings = comment.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        comment = string.Join(Environment.NewLine, strings, 0, 2);
        comment += Sungero.Docflow.ApprovalReviewAssignments.Resources.Ellipsis_Comment;
      }
      
      return comment;
    }
    
    /// <summary>
    /// Получить ошибки валидации подписи.
    /// </summary>
    /// <param name="signature">Электронная подпись.</param>
    /// <returns>Ошибки валидации подписи.</returns>
    public static Structures.ApprovalTask.SignatureValidationErrors GetValidationInfo(Sungero.Domain.Shared.ISignature signature)
    {
      var validationErrors = Structures.ApprovalTask.SignatureValidationErrors.Create(false, false, false);
      if (signature.IsValid)
        return validationErrors;
      
      // Проверить, действителен ли сертификат.
      validationErrors.IsInvalidCertificate = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Certificate);
      
      // Проверить, были ли изменены подписываемые данные.
      validationErrors.IsInvalidData = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Data);
      
      // Проверить, были ли изменены атрибуты подписи.
      validationErrors.IsInvalidAttributes = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Signature);
      
      return validationErrors;
    }
    
    private static void SetIconToBlock(StateBlock signingBlock, Sungero.Domain.Shared.ISignature signature)
    {
      if (signature.IsValid)
      {
        if (signature.SignatureType == Core.SignatureType.Approval)
          signingBlock.AssignIcon(ApprovalTasks.Resources.Sign, StateBlockIconSize.Small);
        else
          signingBlock.AssignIcon(ApprovalTasks.Resources.Approve, StateBlockIconSize.Small);
      }
      else
      {
        if (signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Signature))
          signingBlock.AssignIcon(ApprovalReviewAssignments.Resources.SignatureAttributeChanged, StateBlockIconSize.Small);
        else
          signingBlock.AssignIcon(ApprovalReviewAssignments.Resources.SignedDataChanged, StateBlockIconSize.Small);
      }
    }
    
    /// <summary>
    /// Извлечение данных из подписи.
    /// </summary>
    /// <param name="subject">Тестовая информация о подписи.</param>
    /// <returns>Информация о подписи.</returns>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Obsolete("Используйте метод ParseCertificateSubject.")]
    public static Sungero.Docflow.Structures.Module.ICertificateSubject ParseSignatureSubject(string subject)
    {
      var parsedSubject = Sungero.Docflow.Structures.Module.CertificateSubject.Create();
      var subjectItems = subject.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      
      foreach (var item in subjectItems)
      {
        var itemElements = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
        if (itemElements.Count() < 2)
          continue;
        
        var itemKey = itemElements[0].Trim();
        var itemValue = itemElements[1].Trim();
        
        // Оттримить только одни крайние кавычки.
        if (itemValue.StartsWith("\""))
          itemValue = itemValue.Substring(1);
        if (itemValue.EndsWith("\""))
          itemValue = itemValue.Substring(0, itemValue.Length - 1);

        itemValue = itemValue.Replace("\"\"", "\"");
        
        switch (itemKey)
        {
          case "CN":
            parsedSubject.CounterpartyName = itemValue;
            break;
          case "C":
            parsedSubject.Country = itemValue;
            break;
          case "S":
            parsedSubject.State = itemValue;
            break;
          case "L":
            parsedSubject.Locality = itemValue;
            break;
          case "STREET":
            parsedSubject.Street = itemValue;
            break;
          case "OU":
            parsedSubject.Department = itemValue;
            break;
          case "SN":
            parsedSubject.Surname = itemValue;
            break;
          case "G":
            parsedSubject.GivenName = itemValue;
            break;
          case "T":
            parsedSubject.JobTitle = itemValue;
            break;
          case "O":
            parsedSubject.OrganizationName = itemValue;
            break;
          case "E":
            parsedSubject.Email = itemValue;
            break;
          case "ИНН":
            // Из Synerdocs может прийти ИНН с лидирующими двумя нолями (дополнение ИНН до 12-ти значного формата).
            parsedSubject.TIN = itemValue.StartsWith("00") ? itemValue.Substring(2) : itemValue;
            break;
          default:
            break;
        }
      }
      
      return parsedSubject;
    }
    
    /// <summary>
    /// Получить наименование контрагента из подписи.
    /// </summary>
    /// <param name="subject">Тестовая информация о подписи.</param>
    /// <returns>Наименование контрагента из подписи.</returns>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Public, Obsolete("Используйте метод GetCertificateSignatoryName.")]
    public static string GetCounterpartySignatoryInfo(string subject)
    {
      var certificateSubject = ParseSignatureSubject(subject);
      
      // ФИО.
      var result = certificateSubject.CounterpartyName;
      if (!string.IsNullOrWhiteSpace(certificateSubject.Surname) &&
          !string.IsNullOrWhiteSpace(certificateSubject.GivenName))
        result = string.Format("{0} {1}", certificateSubject.Surname, certificateSubject.GivenName);
      
      return result;
    }
    
    #endregion
    
    /// <summary>
    /// Необходимо ли скрыть "Вынести резолюцию".
    /// </summary>
    /// <returns>True, если скрыть, иначе - false.</returns>
    [Remote(IsPure = true)]
    public bool NeedHideAddResolutionAction()
    {
      // Скрыть вынесение резолюции, если этапа создания поручений нет в правиле.
      var stages = Functions.ApprovalTask.GetStages(ApprovalTasks.As(_obj.Task)).Stages;
      var executionStage = stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Execution);
      if (executionStage == null)
        return true;

      // Скрыть вынесение резолюции, если этап создания поручений схлопнут.
      var isExecutionStageCollapsed = _obj.CollapsedStagesTypesRe.Any(cst => cst.StageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Execution);
      if (isExecutionStageCollapsed)
        return true;
      
      var task = ApprovalTasks.As(_obj.Task);
      
      // Скрыть вынесение резолюции, если у этапа создания поручений нет исполнителя.
      if (Functions.ApprovalStage.GetStagePerformer(task, executionStage.Stage) == null)
        return true;
      
      // Скрыть вынесение резолюции, если это обработка резолюции.
      var reviewStage = stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Review);
      if (reviewStage.Stage.IsResultSubmission == true && !Equals(task.Addressee, _obj.Performer))
        return true;
      
      return false;
    }
    
  }
}