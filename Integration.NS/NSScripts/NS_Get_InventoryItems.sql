SELECT 
	i.id as NetsuiteMaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	i.weight as MaterialWeight
FROM
	item i
WHERE
	i.itemtype = 'InvtPart'