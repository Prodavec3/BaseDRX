using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationGroup;

namespace Sungero.Docflow
{
  partial class RegistrationGroupServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.CanRegisterIncoming.HasValue)
        _obj.CanRegisterIncoming = false;
      if (!_obj.CanRegisterOutgoing.HasValue)
        _obj.CanRegisterOutgoing = false;
      if (!_obj.CanRegisterInternal.HasValue)
        _obj.CanRegisterInternal = false;
      if (!_obj.CanRegisterContractual.HasValue)
        _obj.CanRegisterContractual = false;
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Попытка починить сохранение группы регистрации при параллельной работе.
      lock (RegistrationGroups.Info)
      {
        Functions.RegistrationGroup.GrantRegistrationAccessRights(_obj);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var responsible = _obj.State.Properties.ResponsibleEmployee.OriginalValue;
      var isResponsible = (responsible == null || Recipients.AllRecipientIds.Contains(responsible.Id)) && _obj.AccessRights.CanUpdate();
      if (!isResponsible && !_obj.AccessRights.CanManage())
        e.AddError(RegistrationGroups.Resources.EnoughRights);
      
      if (_obj.CanRegisterIncoming != true &&
          _obj.CanRegisterOutgoing != true &&
          _obj.CanRegisterInternal != true &&
          _obj.CanRegisterContractual != true)
        e.AddError(RegistrationGroups.Resources.ModuleNotSet);
      
      var validateDocumentFlows = Functions.RegistrationGroup.ValidateDocumentFlow(_obj);
      if (!string.IsNullOrWhiteSpace(validateDocumentFlows))
        e.AddError(validateDocumentFlows, RegistrationGroups.Info.Actions.ShowGroupDocumentRegisters);
      
      if (!e.IsValid)
        return;
      
      // Добавление ответственного в список участников.
      if (!_obj.RecipientLinks.Any(r => Equals(r.Member, _obj.ResponsibleEmployee)))
        _obj.RecipientLinks.AddNew().Member = _obj.ResponsibleEmployee;
      
      // Выдать ответственному права на изменение данной группы.
      if (!Equals(responsible, _obj.ResponsibleEmployee))
      {
        if (_obj.ResponsibleEmployee != null)
          _obj.AccessRights.Grant(_obj.ResponsibleEmployee, DefaultAccessRightsTypes.Change);
        // Отобрать права на изменение у предыдущего ответственного.
        if (responsible != null)
          _obj.AccessRights.Revoke(responsible, DefaultAccessRightsTypes.Change);
      }
      
      // Удаляем из "Ответственные за настройку регистрации" сотрудников, которые больше не являются ответственными.
      var managers = Roles.GetAll().FirstOrDefault(r => r.Sid == Constants.Module.RoleGuid.RegistrationManagersRole);
      var responsiblesIds = RegistrationGroups.GetAll(gr => !Equals(gr, _obj) && gr.Status == CoreEntities.DatabookEntry.Status.Active)
        .Select(r => r.ResponsibleEmployee.Id).ToList();
      var oldResponsibles = managers.RecipientLinks.Where(l => !responsiblesIds.Contains(l.Member.Id));
      // Если группа закрыта, то исключить текущего ответственного.
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Active)
        oldResponsibles = oldResponsibles.Where(l => !Equals(l.Member.Id, _obj.ResponsibleEmployee.Id));
      oldResponsibles = oldResponsibles.ToList();
      if (oldResponsibles.Any())
      {
        foreach (var link in oldResponsibles)
          managers.RecipientLinks.Remove(link);
      }
      
      // Добавить ответственного в роль "Ответственные за настройку регистрации".
      if (!Equals(responsible, _obj.ResponsibleEmployee) || !Equals(_obj.Status, _obj.State.Properties.Status.OriginalValue))
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active)
          if (!managers.RecipientLinks.Any(r => Equals(r.Member, _obj.ResponsibleEmployee)))
            managers.RecipientLinks.AddNew().Member = _obj.ResponsibleEmployee;

      // Добавить группу в выбранные роли.
      var isRoleChanged = _obj.State.Properties.CanRegisterIncoming.IsChanged ||
        _obj.State.Properties.CanRegisterOutgoing.IsChanged ||
        _obj.State.Properties.CanRegisterInternal.IsChanged ||
        _obj.State.Properties.CanRegisterContractual.IsChanged;
      var needChangeRoles = isRoleChanged && Users.Current.IncludedIn(Roles.Administrators);
      if (needChangeRoles)
      {
        var includedRoles = new List<IRole>();
        // Добавить в роли безопасности, если группа не закрытая.
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active)
        {
          var needIncludeInClerkRole = _obj.CanRegisterIncoming == true || _obj.CanRegisterInternal == true || _obj.CanRegisterOutgoing == true;
          var nameRole = Roles.GetAll(r => (r.Sid == Constants.Module.RoleGuid.ClerksRole && needIncludeInClerkRole) ||
                                      (r.Sid == Constants.Module.RoleGuid.RegistrationContractualDocument && _obj.CanRegisterContractual == true));
          includedRoles.AddRange(nameRole);
          if (_obj.CanRegisterIncoming == true)
          {
            var registrationIncomingRole = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.RegistrationIncomingDocument).FirstOrDefault();
            if (registrationIncomingRole != null)
              includedRoles.Add(registrationIncomingRole);
          }
          if (_obj.CanRegisterOutgoing == true)
          {
            var registrationOutgoingRole = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.RegistrationOutgoingDocument).FirstOrDefault();
            if (registrationOutgoingRole != null)
              includedRoles.Add(registrationOutgoingRole);
          }
          if (_obj.CanRegisterInternal == true)
          {
            var registrationInternalRole = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.RegistrationInternalDocument).FirstOrDefault();
            if (registrationInternalRole != null)
              includedRoles.Add(registrationInternalRole);
          }
        }
        
        // Добавить группу в выбранные роли.
        foreach (var role in includedRoles)
        {
          if (!role.RecipientLinks.Any(r => Equals(r.Member, _obj)))
            role.RecipientLinks.AddNew().Member = _obj;
        }
        
        // Удаляем группу из ролей, связанных с группами.
        var excludedRoles = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.RegistrationIncomingDocument || r.Sid == Constants.Module.RoleGuid.RegistrationOutgoingDocument ||
                                         r.Sid == Constants.Module.RoleGuid.RegistrationInternalDocument || r.Sid == Constants.Module.RoleGuid.ContractsResponsible ||
                                         r.Sid == Constants.Module.RoleGuid.ClerksRole || r.Sid == Constants.Module.RoleGuid.RegistrationContractualDocument)
          .ToList().Except(includedRoles);
        foreach (var role in excludedRoles)
        {
          var registrationGroupLinks = role.RecipientLinks.Where(r => Equals(r.Member, _obj)).ToList();
          foreach (var link in registrationGroupLinks)
            role.RecipientLinks.Remove(link);
        }
      }
      
    }
  }
}