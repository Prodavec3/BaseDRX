using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  partial class ManagersAssistantFunctions
  {
    /// <summary>
    /// Проверить правильность заполнения карточки ассистента руководителя.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateManagersAssistants(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Руководитель не мб помощником сам у себя.
      if (Equals(_obj.Manager, _obj.Assistant))
        e.AddError(ManagersAssistants.Resources.ManagerCanNotBeAssistantForHimself);

      // Найти дубли.
      if (Functions.ManagersAssistant.HasDuplicatesManagerAssistant(_obj))
      {
        e.AddError(ManagersAssistants.Resources.SelectedAssistantIsAlreadyAppointed);
        return;
      }
      
      if (Functions.ManagersAssistant.HasDuplicatesAssistants(_obj))
        e.AddError(ManagersAssistants.Resources.ExecutiveSecretaryIsAppointed);
      
      if (_obj.IsAssistant != true && _obj.PreparesResolution != true && _obj.PreparesAssignmentCompletion != true && _obj.SendActionItems != true)
        e.AddError(ManagersAssistants.Resources.NoAuthoritiesError);
    }
    
    /// <summary>
    /// Получить ассистентов руководителя.
    /// </summary>
    /// <returns>Ассистенты.</returns>
    public virtual IQueryable<Sungero.Company.IManagersAssistant> GetManagersAssistants()
    {
      return ManagersAssistants
        .GetAll(m => m.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(m => !Equals(m, _obj))
        .Where(m => Equals(m.Manager, _obj.Manager));
    }
    
    /// <summary>
    /// Проверить, что помощник руководителя уже есть.
    /// </summary>
    /// <returns>True - если помощник уже есть, иначе - false.</returns>
    public virtual bool HasDuplicatesAssistants()
    {
      if (_obj.IsAssistant != true)
        return false;
      
      var equalsManagersAssistants = this.GetManagersAssistants()
        .Where(m => m.IsAssistant == true)
        .Any();
      return equalsManagersAssistants;
    }
    
    /// <summary>
    /// Проверить, что уже есть запись справочника для пары Ассистент-Руководитель.
    /// </summary>
    /// <returns>True - если запись есть, иначе - false.</returns>
    public virtual bool HasDuplicatesManagerAssistant()
    {
      var hasDuplicatesAssignmentCompletion = this.GetManagersAssistants()
        .Where(m => Equals(m.Assistant, _obj.Assistant))
        .Any();
      return hasDuplicatesAssignmentCompletion;
    }
    
    /// <summary>
    /// Обновить список сотрудников в роли "Пользователи с расширенным доступом к исполнительской дисциплине".
    /// </summary>
    public virtual void UpdateRoleUsersWithAssignmentCompletionRights()
    {
      var usersWithAssignmentCompletionRights = Roles.GetAll(r => r.Sid == Docflow.PublicConstants.Module.RoleGuid.UsersWithAssignmentCompletionRightsRole).SingleOrDefault();
      
      if (usersWithAssignmentCompletionRights == null)
        return;
      
      int? additionalAssistantId = null;
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.PreparesAssignmentCompletion == true)
        additionalAssistantId = _obj.Assistant.Id;
      this.RemoveAssistantsFromRoleUsersWithAssignmentCompletionRights(additionalAssistantId);
      
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.PreparesAssignmentCompletion == true &&
          (_obj.State.Properties.PreparesAssignmentCompletion.IsChanged || _obj.State.Properties.Assistant.IsChanged || _obj.State.Properties.Status.IsChanged) &&
          !usersWithAssignmentCompletionRights.RecipientLinks.Any(r => Equals(r.Member, _obj.Assistant)))
        usersWithAssignmentCompletionRights.RecipientLinks.AddNew().Member = _obj.Assistant;
    }

    /// <summary>
    /// Удалить пользователей из роли "Пользователи с расширенным доступом к исполнительской дисциплине", если их нет среди ассистентов.
    /// </summary>
    /// <param name="additionalAssistantId">Сотрудник, которого нужно оставить в роли.</param>
    public virtual void RemoveAssistantsFromRoleUsersWithAssignmentCompletionRights(int? additionalAssistantId)
    {
      var usersWithAssignmentCompletionRights = Roles.GetAll(r => r.Sid == Docflow.PublicConstants.Module.RoleGuid.UsersWithAssignmentCompletionRightsRole).SingleOrDefault();
      
      if (usersWithAssignmentCompletionRights == null)
        return;
      
      var assistantIds = ManagersAssistants.GetAll()
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active && a.PreparesAssignmentCompletion == true)
        .Where(a => !Equals(a, _obj))
        .Select(a => a.Assistant.Id)
        .ToList();
      
      if (additionalAssistantId != null)
        assistantIds.Add(additionalAssistantId.Value);
      
      var recipientLinksToRemove = usersWithAssignmentCompletionRights.RecipientLinks.Where(r => !assistantIds.Contains(r.Member.Id)).ToList();
      
      foreach (var recipientLink in recipientLinksToRemove)
        usersWithAssignmentCompletionRights.RecipientLinks.Remove(recipientLink);
    }
  }
}