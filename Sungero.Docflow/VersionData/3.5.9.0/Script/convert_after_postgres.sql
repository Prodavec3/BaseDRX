do $$
begin
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalrule' and column_name = 'rewkperftype')
then
update
  Sungero_Docflow_ApprovalRule
set
  RewkPerfType = 'Author'
where
  RewkPerfType is null;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalrule' and column_name = 'rewkdeadline')
then
update
  Sungero_Docflow_ApprovalRule
set
  RewkDeadline = 3
where
  RewkDeadline is null;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'allowsendrwk')
then
-- Выключить Разрешить отправку на доработку в этапах печати, регистрации, отправке, создании поручений.
update
  Sungero_Docflow_ApprovalStage
set
  AllowSendRwk = false
where
  StageType in ('Print', 'Register', 'Sending', 'Execution')
  and AllowSendRwk is null;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'rewkperftype') 
then
-- Заполнить Отв. за доработку в этапах согласования, согласования с руководителем, подписания, рассмотрения, задание с доработкой.
update
  Sungero_Docflow_ApprovalStage
set
  RewkPerfType = 'FromRule'
where
  RewkPerfType is null
  and (StageType in ('Approvers', 'Manager', 'Sign', 'Review')
  or StageType = 'SimpleAgr' and AllowSendRwk = true);
end if;
  
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'allowchangerwk') 
then
-- Выключить Разрешить выбор ответственного за доработку в этапах печати, регистрации, отправке, создании поручений,
-- согласования, согласования с руководителем, подписания, рассмотрения, задание с доработкой.
update
  Sungero_Docflow_ApprovalStage
set
  AllowChangeRwk = false
where
  StageType in ('Print', 'Register', 'Sending', 'Execution', 'Approvers', 'Manager', 'Sign', 'Review', 'SimpleAgr')
  and AllowChangeRwk is null;
end if;
  
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'reworktype') 
then
-- Заполнить Доработку в этапах согласование с рук., печати, подписания, регистрации, отправке, создании поручений, рассмотрения.
update
  Sungero_Docflow_ApprovalStage
set
  ReworkType = null
where
  ReworkType = 'AfterAll'
  and StageType in ('Manager', 'Print', 'Sign', 'Register', 'Sending', 'Execution', 'Review');
end if;

-- *******************************
-- Заполенение свойств в заданиях.
-- *******************************

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperformer_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании согласования.
update Sungero_WF_Assignment a
set
  RewkPerformer_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerformer_Docflow_Sungero is null
  and a.Discriminator = 'daf1900f-e66b-4368-b724-a073266145d7';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperfcheck_docflow_sungero')
then
-- Заполнить Отв. за доработку в простом задании с доработкой.
update Sungero_WF_Assignment a
set
  RewkPerfCheck_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerfCheck_Docflow_Sungero is null
  and a.Discriminator = 'c09f0ae4-c959-4a57-9895-ae9aaf1f1855';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforexe_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании исполнения поручения.
update Sungero_WF_Assignment a
set
  RewkPerforExe_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforExe_Docflow_Sungero is null
  and a.Discriminator = '495600a5-5f7a-49aa-ac49-9351c9af1109';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforman_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании согласования с руководителем.
update Sungero_WF_Assignment a
set
  RewkPerforMan_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforMan_Docflow_Sungero is null
  and a.Discriminator = 'bbb08f45-60c1-4496-9ff6-b32caed44215';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforprn_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании на печать.
update Sungero_WF_Assignment a
set
  RewkPerforPrn_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforPrn_Docflow_Sungero is null
  and a.Discriminator = '8cd7f587-a910-4e2f-ac4f-afcc15fc3e2f';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforreg_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании на регистрацию.
update Sungero_WF_Assignment a
set
  RewkPerforReg_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforReg_Docflow_Sungero is null
  and a.Discriminator = 'a3b19bde-a0a5-4c7b-9ad4-5a7e800156a9';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforrev_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании на рассмотрение.
update Sungero_WF_Assignment a
set
  RewkPerforRev_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforRev_Docflow_Sungero is null
  and a.Discriminator = '079b6ce1-8a62-41a6-aa89-0de5e5266253';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforsen_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании на отправку КА.
update Sungero_WF_Assignment a
set
  RewkPerforSen_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforSen_Docflow_Sungero is null
  and a.Discriminator = '5d86a6e4-ae51-497a-9122-8a812eba0fc7';
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_wf_assignment' and column_name = 'rewkperforsig_docflow_sungero')
then
-- Заполнить Отв. за доработку в задании на подпись.
update Sungero_WF_Assignment a
set
  RewkPerforSig_Docflow_Sungero = t.Author
from Sungero_WF_Assignment aa
join Sungero_WF_Task t
  on aa.Task = t.Id
where a.Id = aa.Id
  and a.Status = 'InProcess'
  and a.RewkPerforSig_Docflow_Sungero is null
  and a.Discriminator = 'db516acc-0f02-4ea7-960a-08f3f734db4f';
end if;

end $$;