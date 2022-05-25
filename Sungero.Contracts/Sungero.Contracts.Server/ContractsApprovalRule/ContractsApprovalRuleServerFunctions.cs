using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Server
{
  partial class ContractsApprovalRuleFunctions
  {
    
    /// <summary>
    /// Получить права подписи.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="kind">Вид документа.</param>
    /// <param name="minAmount">Ограничение по сумме.</param>
    /// <param name="departments">Отдел.</param>
    /// <returns>Права подписи.</returns>
    /// <Remarks>Не используется.</Remarks>
    [Public, Obsolete]
    public override IQueryable<ISignatureSetting> GetSignatureSetting(IBusinessUnit businessUnit, IDocumentKind kind,
                                                                      double? minAmount, List<Company.IDepartment> departments)
    {
      return this.GetSignatureSettingWithoutDocumentFlowFilter(businessUnit, kind, minAmount, departments)
        .Where(s => s.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.Contracts || s.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.All);
    }
    
    #region Проверка возможности существования маршрутов правила
    
    /// <summary>
    /// Проверка возможности существования маршрута правила.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="ruleConditions">Условие.</param>
    /// <param name="conditionStep">Этап.</param>
    /// <returns>Возможность существования.</returns>
    public override bool CheckRoutePossibility(List<Docflow.Structures.ApprovalRuleBase.RouteStep> route,
                                               List<Docflow.Structures.ApprovalRuleBase.ConditionRouteStep> ruleConditions,
                                               Docflow.Structures.ApprovalRuleBase.RouteStep conditionStep)
    {
      var possibleStage = base.CheckRoutePossibility(route, ruleConditions, conditionStep);
      var conditionType = _obj.Conditions.First(c => c.Number == conditionStep.StepNumber).Condition.ConditionType;
      
      // Проверка условий по типовому договору.
      if (conditionType == Sungero.Contracts.ContractCondition.ConditionType.Standard)
      {
        var standardConditions = this.GetStandardConditionsInRoute(route).Where(c => c.StepNumber != conditionStep.StepNumber).ToList();
        possibleStage = this.CheckStandardConditions(standardConditions, conditionStep);
      }
      
      return possibleStage;
    }

    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по типовому договору.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    public bool CheckStandardConditions(List<Docflow.Structures.ApprovalRuleBase.RouteStep> allConditions, Docflow.Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;
      
      return true;
    }
    
    /// <summary>
    /// Получить все условия по стандартному договору в данном маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <returns>Условия.</returns>
    public List<Docflow.Structures.ApprovalRuleBase.RouteStep> GetStandardConditionsInRoute(List<Docflow.Structures.ApprovalRuleBase.RouteStep> route)
    {
      return route.Where(e => _obj.Conditions.Any(c => Equals(c.Number, e.StepNumber) && c.Condition.ConditionType ==
                                                  Sungero.Contracts.ContractCondition.ConditionType.Standard)).ToList();
    }
    
    #endregion
    
    public override List<IApprovalRoleBase> GetSupportedApprovalRolesForRework()
    {
      return ApprovalRoleBases.GetAll().Where(r => r.Type != Docflow.ApprovalRole.Type.Initiator &&
                                              r.Type != Docflow.ApprovalRole.Type.Approvers &&
                                              r.Type != Docflow.ApprovalRole.Type.Addressee &&
                                              r.Type != Docflow.ApprovalRole.Type.AddrAssistant &&
                                              r.Type != Docflow.ApprovalRole.Type.Signatory &&
                                              r.Type != Docflow.ApprovalRole.Type.SignAssistant)
        .ToList();
    }
  }
}