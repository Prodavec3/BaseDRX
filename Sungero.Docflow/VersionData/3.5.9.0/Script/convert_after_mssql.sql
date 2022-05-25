if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalRule' and column_name = 'RewkPerfType')
-- Отв. за доработку по умолчанию Инициатор.
update
  Sungero_Docflow_ApprovalRule
set
  RewkPerfType = 'Author'
where
  RewkPerfType is null

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalRule' and column_name = 'RewkDeadline')
-- Срок доработки по умолчанию 3 дня.
update
  Sungero_Docflow_ApprovalRule
set
  RewkDeadline = 3
where
  RewkDeadline is null
 
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'AllowSendRwk') 
-- Выключить Разрешить отправку на доработку в этапах печати, регистрации, отправке, создании поручений.
update
  Sungero_Docflow_ApprovalStage
set
  AllowSendRwk = 0
where
  StageType in ('Print', 'Register', 'Sending', 'Execution')
  and AllowSendRwk is null
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'RewkPerfType') 
-- Заполнить Отв. за доработку в этапах согласования, согласования с руководителем, подписания, рассмотрения, задание с доработкой.
update
  Sungero_Docflow_ApprovalStage
set
  RewkPerfType = 'FromRule'
where
  RewkPerfType is null
  and (StageType in ('Approvers', 'Manager', 'Sign', 'Review')
  or StageType = 'SimpleAgr' and AllowSendRwk = 1)
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'AllowChangeRwk') 
-- Выключить Разрешить выбор ответственного за доработку в этапах печати, регистрации, отправке, создании поручений,
-- согласования, согласования с руководителем, подписания, рассмотрения, задание с доработкой.
update
  Sungero_Docflow_ApprovalStage
set
  AllowChangeRwk = 0
where
  StageType in ('Print', 'Register', 'Sending', 'Execution', 'Approvers', 'Manager', 'Sign', 'Review', 'SimpleAgr')
  and AllowChangeRwk is null
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'ReworkType') 
-- Заполнить Доработку в этапах согласование с рук., печати, подписания, регистрации, отправке, создании поручений, рассмотрения.
update
  Sungero_Docflow_ApprovalStage
set
  ReworkType = null
where
  ReworkType = 'AfterAll'
  and StageType in ('Manager', 'Print', 'Sign', 'Register', 'Sending', 'Execution', 'Review')
  
-- *******************************
-- Заполенение свойств в заданиях.
-- *******************************

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerformer_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании согласования.
update a
set
  RewkPerformer_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerformer_Docflow_Sungero is null
  and a.Discriminator = 'daf1900f-e66b-4368-b724-a073266145d7'
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerfCheck_Docflow_Sungero')
-- Заполнить Отв. за доработку в простом задании с доработкой.
update a
set
  RewkPerfCheck_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerfCheck_Docflow_Sungero is null
  and a.Discriminator = 'c09f0ae4-c959-4a57-9895-ae9aaf1f1855'
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforExe_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании исполнения поручения.
update a
set
  RewkPerforExe_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforExe_Docflow_Sungero is null
  and a.Discriminator = '495600a5-5f7a-49aa-ac49-9351c9af1109'
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforMan_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании согласования с руководителем.
update a
set
  RewkPerforMan_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforMan_Docflow_Sungero is null
  and a.Discriminator = 'bbb08f45-60c1-4496-9ff6-b32caed44215'

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforPrn_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании на печать.
update a
set
  RewkPerforPrn_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforPrn_Docflow_Sungero is null
  and a.Discriminator = '8cd7f587-a910-4e2f-ac4f-afcc15fc3e2f'

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforReg_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании на регистрацию.
update a
set
  RewkPerforReg_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforReg_Docflow_Sungero is null
  and a.Discriminator = 'a3b19bde-a0a5-4c7b-9ad4-5a7e800156a9'

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforRev_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании на рассмотрение.
update a
set
  RewkPerforRev_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforRev_Docflow_Sungero is null
  and a.Discriminator = '079b6ce1-8a62-41a6-aa89-0de5e5266253'

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforSen_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании на отправку КА.
update a
set
  RewkPerforSen_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforSen_Docflow_Sungero is null
  and a.Discriminator = '5d86a6e4-ae51-497a-9122-8a812eba0fc7'

if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_WF_Assignment' and column_name = 'RewkPerforSig_Docflow_Sungero')
-- Заполнить Отв. за доработку в задании на подпись.
update a
set
  RewkPerforSig_Docflow_Sungero = t.Author
from Sungero_WF_Assignment a
join Sungero_WF_Task t
  on a.Task = t.Id
where a.Status = 'InProcess'
  and a.RewkPerforSig_Docflow_Sungero is null
  and a.Discriminator = 'db516acc-0f02-4ea7-960a-08f3f734db4f'