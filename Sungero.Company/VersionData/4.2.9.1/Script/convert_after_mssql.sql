update sungero_company_assistant
set  isassistant = 1
where isassistant is null;

update sungero_company_assistant
set  preparesasgcmp = 0
where preparesasgcmp is null;