using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.VisibilityRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class VisibilityRuleFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.CoreEntities.IRecipient> RecipientFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query, Sungero.Domain.FilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return Functions.Module.ExcludeSystemRecipients(query, false);
    }

    public virtual IQueryable<Sungero.CoreEntities.IRecipient> VisibleMemberFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query, Sungero.Domain.FilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return Functions.Module.ExcludeSystemRecipients(query, false);
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.Recipient != null)
      {
        var ids = Users.OwnRecipientIdsFor(_filter.Recipient).ToList();
        var headRecipients = PublicFunctions.Module.GetHeadRecipientsByEmployee(_filter.Recipient.Id);
        ids.AddRange(headRecipients);
        query = query.Where(q => q.Recipients.Any(r => ids.Contains(r.Recipient.Id)));
      }
      
      if (_filter.VisibleMember != null)
      {
        var ids = Users.OwnRecipientIdsFor(_filter.VisibleMember).ToList();
        var headRecipients = PublicFunctions.Module.GetHeadRecipientsByEmployee(_filter.VisibleMember.Id);
        ids.AddRange(headRecipients);
        query = query.Where(q => q.VisibleMembers.Any(r => ids.Contains(r.Recipient.Id)) && !q.ExcludedMembers.Any(r => ids.Contains(r.Recipient.Id)));
      }

      return query;
    }
  }

  partial class VisibilityRuleExcludedMembersRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExcludedMembersRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return (IQueryable<T>)Functions.Module.ExcludeSystemRecipients(query, true);
    }
  }

  partial class VisibilityRuleVisibleMembersRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> VisibleMembersRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return (IQueryable<T>)Functions.Module.ExcludeSystemRecipients(query, true);
    }
  }

  partial class VisibilityRuleRecipientsRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RecipientsRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return (IQueryable<T>)Functions.Module.ExcludeSystemRecipients(query, true);
    }
  }

}