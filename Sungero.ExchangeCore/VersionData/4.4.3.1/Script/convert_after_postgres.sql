update sungero_excore_exchangeservic
set status = 'Closed'
where exchprovider = 'Synerdocs' and status = 'Active';

update sungero_excore_boxbase
set status = 'Closed', 
	connectionstat = null
where status = 'Active' and exchangeservic in (select id from sungero_excore_exchangeservic where exchprovider = 'Synerdocs');

update sungero_excore_boxbase boxbase
set status = 'Closed', 
	connectionstat = null
from sungero_excore_boxbase rootbox
where boxbase.status = 'Active' and rootbox.id = boxbase.rootbox 
	and rootbox.status = 'Closed' and rootbox.exchangeservic in (select id from sungero_excore_exchangeservic where exchprovider = 'Synerdocs');