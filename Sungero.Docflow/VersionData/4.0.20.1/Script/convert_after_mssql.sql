-- Установить свойство IsManyAddresses в False.
update Sungero_Content_EDoc
set InIsManyAddr_Docflow_Sungero = 'FALSE'
where Discriminator = '8DD00491-8FD0-4A7A-9CF3-8B6DC2E6455D' 
  and InIsManyAddr_Docflow_Sungero IS NULL

-- Синхронизировать единичного адресата в коллекцию адресатов.
if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_InAddressees')
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
  doc.InAddressee_Docflow_Sungero,
  emp.Department_Company_Sungero
from Sungero_Content_EDoc as doc
  left join Sungero_Docflow_InAddressees addr on doc.Id = addr.Edoc
  left join Sungero_Core_Recipient emp on doc.InAddressee_Docflow_Sungero = emp.Id
where doc.Discriminator = '8DD00491-8FD0-4A7A-9CF3-8B6DC2E6455D'
  and doc.InAddressee_Docflow_Sungero is not null
  and addr.EDoc is null

-- Дискриминатор свойства-коллекции адресатов входящего письма (9EE5DA19-7A73-4C27-A743-EB68981C6EC2).
declare @addresseesDiscriminator varchar(36) = '9EE5DA19-7A73-4C27-A743-EB68981C6EC2'
declare @addresseesNewId int
declare @documentToConvertCount int
declare @InAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @documentToConvertCount = COUNT(DocId)
from @temp_AddresseesTable 
   
-- Получение последнего ИД в таблице для свойства-коллекции адресатов входящего письма.
select @InAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_Docflow_InAddressees'
   
-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_Docflow_InAddressees', @addresseesNewId output, @documentToConvertCount

insert into Sungero_Docflow_InAddressees
(
  [Id],
  [Discriminator],
  [EDoc],
  [Number],
  [Addressee], 
  [Department]
)
select
  @InAddresseesTableLastId + t.Id,
  @addresseesDiscriminator,
  t.DocId,
  1,
  t.AddresseeId,
  t.DepartmentId
from @temp_AddresseesTable as t

end

-- Заполнить ManyAddressesLabel у одноадресных входящих писем.
update doc
set doc.ManyAddrLabel_Docflow_Sungero = recip.Name
from Sungero_Content_EDoc doc
  left join Sungero_Core_Recipient recip
  on doc.InAddressee_Docflow_Sungero = recip.Id
where doc.InIsManyAddr_Docflow_Sungero = 0
  and doc.InAddressee_Docflow_Sungero is not null

-- Установить свойство IsAutoExecLeadingActionItem в False.
update sungero_docflow_personsetting
set ExecLeadAI = 'false'
where ExecLeadAI is null