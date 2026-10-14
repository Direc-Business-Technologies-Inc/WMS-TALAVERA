CRF 1

Add Indicator for Printed Printouts for Receiving Reports, Return to Supplier, Stock Transfer Requests, Packing Labelsand Trip Tickets in the WMS Web 

Details:
Add a “Printed” indicator in the WMS that is automatically flagged once a transaction’s printout has been generated, and display the number of times the record on the transaction record so users can immediately tell whether a document has already been printed.

Reason for change:
Warehouse users may print the same document more than once or are unsure whether it has already been processed. This results in duplicate paperwork. A printed indicator improves traceability of printed documents and removes the need for manual checking



CRF 2

Update Posting of GR and RTS to Include Purchase Price and Amount

Details:
Update the posting of Goods Receipt (GR) and Return to Supplier (RTS) transactions to add Purchase Price and Amount columns per line. The Purchase Price shall be determined based on the following priority: (1) the Purchase Price of the preferred vendor per line; (2) the item’s Average Cost if the Purchase Price does not exist; and (3) zero if neither is available. The Amount
shall be computed based on the resulting Purchase Price.

Reason for change:Posted GR and RTS lines do not show the cost of the goods received from or returned to suppliers. Adding the purchase price and amount ensures that every line carries a consistent cost value.



CRF 3 

Auto Fulfill STR for Returns

Details:
Add an automatic fulfillment process for Stock Transfer Returns (STR) – Returns upon approval, using a Custom Record as a maintenance table to identify which source and destination locations need to be matched to trigger the auto-fulfillment
process.

Reason for change:
The current process requires the user to pack and ship items from STRs, doubling the work needed to move items from locations to each subsidiary’s BO Warehouse.



CRF 4

Editable Date Field for All Transactions with Current Date as Default

Details:
Add an editable Date field to all WMS transactions, with the current date as the default value upon creation. Users may change the date when needed, instead of the transaction date being fixed to the date of creation.

Reason for change:
Fixing the transaction date to the date of creation does not accommodate transactions that are processed or encoded after the actual warehouse activity, such as late encoding or backdated entries. An editable date that defaults to the current date lets users record the correct transaction date when needed while keeping data entry quick for regular transactions.



CRF 5

Display Created From STR No. in the Body Fields and Table of RTS in the WMS Web	
Details:
Display the STR number from which an RTS was created (“Created From STR No.”) in both the body fields and the line table of the RTS transaction in the WMS Web.	

Reason for change:
Users cannot easily trace which STR an RTS originated from and have to look for the source transaction manually. Showing the source STR number improves traceability and
speeds up verification.



CRF 6

Replace Weight Columns with Original STR Quantity in STR Receiving in the WMS Web

Details:
Remove the weight columns in the STR Receiving screen of the WMS Web and add an Original STR Quantity column in their place, showing the quantity
originally indicated in the STR.	

Reason for change:
The original STR quantity is the reference users need to check received quantities against what was transferred.



CRF 7

Include From and To Subsidiary and Location in the Fulfillment Selection in Trip Ticket in the WMS Web

Details:
Include the From Subsidiary, From Location, To Subsidiary and To Location in the fulfillment selection of the Trip Ticket in the WMS Web, so users can see the source and destination of each fulfillment when selecting which ones to include.	

Reason for change:
The fulfillment selection does not show where a fulfillment is coming from or going to, so users have to open each fulfillment to check. Showing the subsidiary and location on both ends allows users to select the right fulfillments faster and
reduces the risk of wrong trip assignments.