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
      return Functions.Module.CreateApprovalListStateView(document);
    }
    
    /// <summary>
    /// Получить ошибки валидации подписи.
    /// </summary>
    /// <param name="signature">Электронная подпись.</param>
    /// <returns>Ошибки валидации подписи.</returns>
    public static Structures.ApprovalTask.SignatureValidationErrors GetValidationInfo(Sungero.Domain.Shared.ISignature signature)
    {
      return Functions.Module.GetValidationInfo(signature);
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
            // Может прийти ИНН с лидирующими двумя нолями (дополнение ИНН до 12-ти значного формата).
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