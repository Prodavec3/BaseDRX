-- Заполнить свойство RegistrationMarkPosition значением по умолчанию.
update Sungero_Docflow_PersonSetting
set RegMarkPos ='BottomRight'
where RegMarkPos is null