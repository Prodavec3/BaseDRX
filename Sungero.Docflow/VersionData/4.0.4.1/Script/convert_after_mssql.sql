if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalRule' and column_name = 'NeedRestrInAR') 
-- Выключить Ограничить права инициатора на документ и приложения.
update
  Sungero_Docflow_ApprovalRule
set
  NeedRestrInAR = 0
where 
  NeedRestrInAR is null
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'NeedRestrPerAR') 
-- Выключить Ограничить права исполнителя после выполнения задания.
update
  Sungero_Docflow_ApprovalStage
set
  NeedRestrPerAR = 0
where 
  NeedRestrPerAR is null
  
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_ApprovalStage' and column_name = 'RightsType') 
-- Установить тип прав по умолчанию.
update
  Sungero_Docflow_ApprovalStage
set
  RightsType = case when StageType = 'Print' then 'Read' else 'Edit' end
where 
  RightsType is null