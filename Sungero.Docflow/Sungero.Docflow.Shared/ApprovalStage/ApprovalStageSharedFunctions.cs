using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalStageFunctions
  {
    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      #region Шпаргалка обязательности свойств
      /* (*) - должно быть заполнено хотя бы одно из указанных.
         (**) - зависит от значений галочки, включающей данную функциональность.
       
                                        Согл руков | Согл обяз. | Согл доп.согл | Подписание | Регистрация | Отправка КА | Задание | Контроль возврата| Печать | Уведомление | Расс.| Соз.пор.|
        ---Один исполнитель--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        Тип:                                   +(*)                                     +          +(*)         +(*)                                     +(*)       +(пом)       +      +(*)
        Роль       :                           +(*)                                     +          +(*)         +(*)                                     +(*)       +(пом)       +      +(*)
        Исполнитель:                           +(*)                                     +          +(*)         +(*)                                     +(*)       +(пом)       +      +(*)
        ---Несколько исполнителей--------------------------------------------------------------------------------------------------------------------------------------------------------------
        Исполнители:                                       +                                                                 +(*)          +(*)                      +(*)
        Роли:                                              +                                                                 +(*)          +(*)                      +(*)
        Старт:                                             +              +                                                  +
        ----Срок и прочее----------------------------------------------------------------------------------------------------------------------------------------------------------------------
        Срок дней:                             +           +              +            +            +            +           +             +             +                       +        +
        Срок часов:                            +           +              +            +            +            +           +             +             +                       +        +
        Доработка:                                         +              +                                                  +
        Тема:                                                                                                                +                           +           +
        Отв. за доработку:                     +           +              +            +            +(**)        +(**)       +(**)                       +(**)                   +        +(**)
       */
      
      #endregion
      
      var type = _obj.StageType;
      var isApprovers = type == StageType.Approvers;
      var isRegister = type == StageType.Register;
      var isManager = type == StageType.Manager;
      var isSending = type == StageType.Sending;
      var isSign = type == StageType.Sign;
      var isPrint = type == StageType.Print;
      var isSimpleAgreement = type == StageType.SimpleAgr;
      var isExecution = type == StageType.Execution;
      var isNotice = type == StageType.Notice;
      var isReview = type == StageType.Review;
      
      var assigneeRequired = isManager || isRegister || isPrint || isExecution || isSending || isSign || isReview;
      var isEmployee = _obj.AssigneeType == AssigneeType.Employee;
      var isRole = _obj.AssigneeType == AssigneeType.Role;
      
      _obj.State.Properties.AssigneeType.IsRequired = assigneeRequired;
      _obj.State.Properties.Assignee.IsRequired = assigneeRequired && isEmployee;
      _obj.State.Properties.ApprovalRole.IsRequired = assigneeRequired && isRole;
      
      // Свойство "Старт".
      _obj.State.Properties.Sequence.IsRequired = _obj.Info.Properties.Sequence.IsRequired ||
        (isApprovers || isSimpleAgreement);
      
      // Свойство "Доработка".
      var allowSendToReworkInSimpleAgreement = isSimpleAgreement && _obj.AllowSendToRework.Value;
      var isStageWithRework = isApprovers || allowSendToReworkInSimpleAgreement;
      _obj.State.Properties.ReworkType.IsRequired = _obj.Info.Properties.ReworkType.IsRequired || isStageWithRework;
      
      // Свойство "Тема".
      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired ||
        (isNotice || isSimpleAgreement);
      
      // Срок.
      _obj.State.Properties.DeadlineInDays.IsRequired = !(isNotice || _obj.DeadlineInHours.HasValue);
      _obj.State.Properties.DeadlineInHours.IsRequired = !(isNotice || _obj.DeadlineInDays.HasValue) &&
        _obj.State.Properties.DeadlineInHours.IsEnabled;
      
      // Отв. за доработку.
      var canChangeReworkPerformer = isApprovers || isManager || isSign || isReview || (_obj.AllowSendToRework ?? false);
      _obj.State.Properties.ReworkPerformerType.IsRequired = canChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsRequired = _obj.ReworkPerformerType == Sungero.Docflow.ApprovalStage.ReworkPerformerType.EmployeeRole && canChangeReworkPerformer;
      _obj.State.Properties.ReworkApprovalRole.IsRequired = _obj.ReworkPerformerType == Sungero.Docflow.ApprovalStage.ReworkPerformerType.ApprovalRole && canChangeReworkPerformer;
    }
    
    /// <summary>
    /// Установить видимость свойств.
    /// </summary>
    public virtual void SetPropertiesVisibility()
    {
      #region Шпаргалка
      /*                                   Согл рук | Согл обяз. | Согл доп.согл | Подписание | Регистрация | Отправка КА | Задание | Контроль возврата| Уведомление| Печать | Рассм. | Соз.пор. |
       * ---Один исполнитель------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       * Тип:                                   +                                       +            +             +                                                    +        +          +
       * Исполнитель:                           +                                       +            +             +                                                    +        +          +
       * Роль:                                  +                                       +            +             +                                                    +        +          +
       * ---Несколько исполнителей------------------------------------------------------------------------------------------------------------------------------------------------------------------
       * Исполнители:                                     +                                                                   +             +               +
       * Старт:                                           +              +                                                     +
       * ----Флажки---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       * Внесение результата рассмотрения                                                                                                                                        +
       * Требовать усиленную подпись:           +         +              +              +
       * Подтверждение подписания:                                                      +
       * Разрешить доп. согласующих                       +
       * Разрешить отправку на доработку:                                                            +             +           +                                        +                   +
       * Разрешить выбор отв. за доработку:     +         +              +              +            +             +           +                                        +        +          +
       * Разрешить согласование с замечаниями:  +         +              +
       * ----Срок и прочее--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       * Срок дней:                             +         +              +              +             +            +           +             +                          +        +          +
       * Срок часов:                            +         +              +              +             +            +           +             +                          +        +          +
       * Отсрочка старта в днях:                +         +              +              +             +            +           +             +                          +        +          +
       * Доработка:                                       +              +                            +            +           +                                        +                   +
       * Тема:                                                                                                                 +                              +
       * Отв. за доработку:                     +         +              +              +             +            +           +                                        +        +          +
       * Сотрудник/роль:                        +         +              +              +             +            +           +                                        +        +          +
       */
      #endregion
      
      var properties = _obj.State.Properties;
      var type = _obj.StageType;
      var isControlReturn = type == StageType.CheckReturn;
      var isApprovers = type == StageType.Approvers;
      var isManager = type == StageType.Manager;
      var isSending = type == StageType.Sending;
      var isSign = type == StageType.Sign;
      var isSimpleAssignment = type == StageType.SimpleAgr;
      var isNotice = type == StageType.Notice;
      var isReview = type == StageType.Review;
      var isExecution = type == StageType.Execution;
      var allowSendToRework = _obj.AllowSendToRework ?? false;
      var isEmployee = _obj.AssigneeType == AssigneeType.Employee;
      var isRole = !isEmployee;
      var isNotificationOrSimpleAssignment = isNotice || isSimpleAssignment;
      var isRegistering = type == StageType.Register;
      var isPrinting = type == StageType.Print;
      
      var isMultiPerformers = isApprovers || isControlReturn || isSimpleAssignment || isNotice;
      var isSinglePerformer = !isMultiPerformers && type.HasValue;
      var hasRework = isManager || isApprovers || isSign || isSending || isRegistering || isPrinting || isExecution || isReview || isSimpleAssignment;
      var isReworkApprovalRole = _obj.ReworkPerformerType == ReworkPerformerType.ApprovalRole;
      
      // Один исполнитель.
      properties.AssigneeType.IsVisible = isSinglePerformer;
      properties.Assignee.IsVisible = isSinglePerformer && isEmployee;
      properties.ApprovalRole.IsVisible = isSinglePerformer && isRole;
      
      // Несколько исполнителей.
      properties.Recipients.IsVisible = isMultiPerformers;
      properties.ApprovalRoles.IsVisible = isMultiPerformers;
      properties.Sequence.IsVisible = isMultiPerformers && !isControlReturn && !isNotice;
      
      // Флажки.
      properties.IsResultSubmission.IsVisible = isReview;
      properties.NeedStrongSign.IsVisible = isManager || isApprovers || isSign || isReview;
      properties.AllowSendToRework.IsVisible = isSimpleAssignment || isSending || isRegistering || isPrinting || isExecution;
      properties.IsConfirmSigning.IsVisible = isSign;
      properties.AllowAdditionalApprovers.IsVisible = isApprovers;
      properties.AllowApproveWithSuggestions.IsVisible = isManager || isApprovers;
      
      // Срок и прочее.
      properties.DeadlineInDays.IsVisible = !isNotice;
      properties.DeadlineInHours.IsVisible = !isNotice;
      properties.StartDelayDays.IsVisible = isControlReturn;
      properties.ReworkType.IsVisible = isApprovers || isSimpleAssignment || isSending || isRegistering || isPrinting || isExecution;
      properties.Subject.IsVisible = isSimpleAssignment || isNotice;
      
      properties.ReworkPerformerType.IsVisible = hasRework;
      properties.AllowChangeReworkPerformer.IsVisible = hasRework;
      properties.ReworkPerformer.IsVisible = hasRework && !isReworkApprovalRole;
      properties.ReworkApprovalRole.IsVisible = hasRework && isReworkApprovalRole;
    }
    
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public virtual void SetPropertiesAvailability()
    {
      var type = _obj.StageType;
      var isControlReturn = type == StageType.CheckReturn;
      var isSimpleAgreement = type == StageType.SimpleAgr;
      var allowSendToRework = _obj.AllowSendToRework ?? false;
      var isSign = type == StageType.Sign;
      var isReview = type == StageType.Review;
      var isEmployee = _obj.AssigneeType == AssigneeType.Employee;
      var isRole = _obj.AssigneeType == AssigneeType.Role;
      var needAssistant = _obj.IsConfirmSigning == true || _obj.IsResultSubmission == true;
      var isApprovers = type == StageType.Approvers;
      var isManager = type == StageType.Manager;
      var canChangeReworkPerformer = isApprovers || isManager || isSign || isReview || allowSendToRework;
      var isRegister = type == StageType.Register;
      var isSending = type == StageType.Sending;
      var isNotice = type == StageType.Notice;
      
      _obj.State.Properties.DeadlineInHours.IsEnabled = !isControlReturn;
      
      // Подтверждение подписания или внесение результатов рассмотрения.
      var isEnable = true;
      if (isSign || isReview)
        isEnable = needAssistant;
      
      _obj.State.Properties.AssigneeType.IsEnabled = isEnable;
      _obj.State.Properties.Assignee.IsEnabled = isEnable && isEmployee;
      _obj.State.Properties.ApprovalRole.IsEnabled = isEnable && isRole;
      _obj.State.Properties.ReworkType.IsEnabled = isSimpleAgreement ? allowSendToRework : isApprovers;
      _obj.State.Properties.ReworkPerformerType.IsEnabled = canChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsEnabled = _obj.ReworkPerformerType == Sungero.Docflow.ApprovalStage.ReworkPerformerType.EmployeeRole && canChangeReworkPerformer;
      _obj.State.Properties.AllowChangeReworkPerformer.IsEnabled = canChangeReworkPerformer && _obj.ReworkType != ReworkType.AfterAll;
      
      // Тип прав.
      _obj.State.Properties.RightType.IsEnabled = !isSending;
      
      // Ограничить права исполнителя после выполнения задания.
      _obj.State.Properties.NeedRestrictPerformerRights.IsEnabled = _obj.RightType != RightType.Read && !isNotice;
    }
    
    #region Привязка ролей к типам этапов
    
    /// <summary>
    /// Получить список ролей, доступных для этого этапа.
    /// </summary>
    /// <returns>Список ролей.</returns>
    public virtual List<Enumeration?> GetPossibleRoles()
    {
      var roleTypes = new List<Enumeration?>();
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Manager)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.InitManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DepartManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocDepManager);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Approvers)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.InitManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectAdmin);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.CompResponsible);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DepartManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocDepManager);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Sign && _obj.IsConfirmSigning == false)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Signatory);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Sign && _obj.IsConfirmSigning == true)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Register)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Sending)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.PrintResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectAdmin);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.CompResponsible);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.SimpleAgr || _obj.StageType == Docflow.ApprovalStage.StageType.Notice)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Addressee);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Addressees);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Signatory);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.InitManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.PrintResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectAdmin);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Approvers);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.CompResponsible);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DepartManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocDepManager);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.CheckReturn)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContRespManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectAdmin);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Approvers);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.CompResponsible);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Print)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ContractResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.PrintResp);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.ProjectAdmin);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Review && _obj.IsResultSubmission == false)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Addressee);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Review && _obj.IsResultSubmission == true)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.InitManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DepartManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocDepManager);
      }
      
      if (_obj.StageType == Docflow.ApprovalStage.StageType.Execution)
      {
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Initiator);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Addressee);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.Signatory);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.InitManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.OutDocRegister);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DepartManager);
        roleTypes.Add(Docflow.ApprovalRoleBase.Type.DocDepManager);
      }
      
      return roleTypes;
    }
    
    #endregion
    
    /// <summary>
    /// Установить роль по умолчанию.
    /// </summary>
    public void SetDefaultRole()
    {
      var type = _obj.StageType;
      if (type == null)
        return;
      
      if (type == StageType.Manager)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.InitManager);
      
      if (type == StageType.Execution)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.AddrAssistant);
      
      if (type == StageType.Print)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.PrintResp);
      
      if (type == StageType.Register)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.DocRegister);
      
      if (type == StageType.Review)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Addressee);
      
      if (type == StageType.Sign)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Signatory);
      
      if (type == StageType.Sending)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.OutDocRegister);
    }
    
    /// <summary>
    /// Проверить использование роли в этапе.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <returns>True, если роль используется в этапе.</returns>
    public bool HasRole(Enumeration? roleType)
    {
      if (!roleType.HasValue)
        return false;
      
      if (_obj.ApprovalRoles.Any(r => Equals(r.ApprovalRole.Type, roleType)))
        return true;

      if (_obj.ApprovalRole != null && Equals(_obj.ApprovalRole.Type, roleType))
        return true;

      if (_obj.ReworkApprovalRole != null && Equals(_obj.ReworkApprovalRole.Type, roleType))
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Получить представление срока в этапе.
    /// </summary>
    /// <param name="performersCount">Количество исполнителей.</param>
    /// <returns>Представление срока.</returns>
    public string GetDeadlineDescription(int performersCount)
    {
      return this.GetDeadlineDescription(performersCount, " ", true);
    }
    
    /// <summary>
    /// Получить представление срока в этапе.
    /// </summary>
    /// <param name="performersCount">Количество исполнителей.</param>
    /// <param name="daysHoursSeparator">Разделитель дней и часов.</param>
    /// <param name="needHoursConvert">Конвертировать часы в дни.</param>
    /// <returns>Представление срока.</returns>
    public string GetDeadlineDescription(int performersCount, string daysHoursSeparator, bool needHoursConvert)
    {
      var isParallel = _obj.Sequence != Sequence.Serially;
      return Functions.ApprovalStageBase.GetDeadlineDescription(_obj, performersCount, daysHoursSeparator, needHoursConvert, isParallel);
    }
    
    public override Enumeration? GetStageType()
    {
      return _obj.StageType;
    }
  }
}