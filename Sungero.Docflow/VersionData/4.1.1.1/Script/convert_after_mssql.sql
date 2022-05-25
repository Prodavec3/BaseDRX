-- Установить свойство IsManyAddressees в служебной записке в False.
update Sungero_Content_EDoc
set MemoIsManyAddr_Docflow_Sungero = 'false'
where Discriminator = '95af409b-83fe-4697-a805-5a86ceec33f5' 
  and MemoIsManyAddr_Docflow_Sungero IS NULL

-- Синхронизировать единичного адресата в коллекцию адресатов.
if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_MemoAddressees')
begin

declare @temp_AddresseesTable table
(
  Id int identity(1,1),
  DocId int,
  AddresseeId int,
  DepartmentId int
)

insert into @temp_AddresseesTable
select distinct
  doc.Id,
  doc.Addressee_Docflow_Sungero,
  emp.Department_Company_Sungero
from Sungero_Content_EDoc as doc
  left join Sungero_Docflow_MemoAddressees addr on doc.Id = addr.Edoc
  left join Sungero_Core_Recipient emp on doc.Addressee_Docflow_Sungero = emp.Id
where doc.Discriminator = '95af409b-83fe-4697-a805-5a86ceec33f5'
  and doc.Addressee_Docflow_Sungero is not null
  and addr.EDoc is null

-- Дискриминатор свойства-коллекции адресатов служебной записки (5c815151-6faf-4b26-a47a-bd82f68c0c23).
declare @addresseesDiscriminator varchar(36) = '5c815151-6faf-4b26-a47a-bd82f68c0c23'
declare @addresseesNewId int
declare @documentToConvertCount int
declare @MemoAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @documentToConvertCount = COUNT(DocId)
from @temp_AddresseesTable 
   
-- Получение последнего ИД в таблице для свойства-коллекции адресатов служебной записки.
select @MemoAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_Docflow_MemoAddressees'
   
-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_Docflow_MemoAddressees', @addresseesNewId output, @documentToConvertCount

insert into Sungero_Docflow_MemoAddressees
(
  [Id],
  [Discriminator],
  [EDoc],
  [Number],
  [Addressee], 
  [Department]
)
select
  @MemoAddresseesTableLastId + t.Id,
  @addresseesDiscriminator,
  t.DocId,
  1,
  t.AddresseeId,
  t.DepartmentId
from @temp_AddresseesTable as t

end

-- Заполнить ManyAddresseesLabel у одноадресных служебных записок.
update doc
set doc.MemoManyAddrLb_Docflow_Sungero = recip.Name
from Sungero_Content_EDoc doc
  left join Sungero_Core_Recipient recip
  on doc.Addressee_Docflow_Sungero = recip.Id
where doc.MemoIsManyAddr_Docflow_Sungero = 0
  and doc.Addressee_Docflow_Sungero is not null
  and doc.MemoManyAddrLb_Docflow_Sungero is null

-- Заполнение поля StageBase из значения поля Stage.
if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Docflow_RuleStages' and column_name = 'StageBase') 
 update Sungero_Docflow_RuleStages
 set StageBase = Stage
 where StageBase is null