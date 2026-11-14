# Requirements — Receiving and Packing Print Workflows

**Date:** 2026-10-30  
**Status:** Confirmed  
**Mode:** FEATURE  
**Scope:** Move standalone Receiving Report and Packing Label workflows into transaction tabs; add print-status views to every Packing tab; add per-record print buttons to every tab inside Receiving and Packing; investigate and fix inaccurate Printed counts across all pages using print-status views.  
**Related:** None found

## Introduction

Users currently access Receiving Reports and Packing Labels through standalone tabs, while print actions and print-status controls vary across transaction tabs. This feature moves users into the relevant transaction context for printing, provides consistent Packing status views, and corrects Printed counts across pages using print-status views.

## Glossary

- **Transaction tab:** A category or document tab within Receiving or Packing, such as Purchase Orders, Stock Transfer Request, Returns, or Returns to Supplier.
- **Print-status view:** A view of records in one of three groups: For Printing, Printed, or All.
- **Printout:** A document generated from a transaction, such as a Receiving Report or Packing Label.
- **Print-count field:** The NetSuite field currently used by WMS to determine whether a record has been printed.

## 1. Transaction-specific print workflows

### 1.1 User story

As a warehouse user, I want to find and print an individual document from its transaction tab, so I can print the specific transaction I am working with.

### 1.2 Acceptance criteria

- **WHEN** a user opens an applicable transaction tab **THEN** the page **SHALL** provide For Printing, Printed, and All views for records in that transaction context.
- **WHEN** a user selects a print-status view **THEN** the grid **SHALL** show records matching both that view and the transaction category.
- **WHEN** a user prints a record **THEN** the print action **SHALL** use the identifier for that specific record.
- **WHEN** a transaction tab has an existing supported print action **THEN** its print action **SHALL** remain available.
- **WHEN** a transaction type has no confirmed supported printout **THEN** implementation **SHALL** verify support and mapping before exposing a misleading or invalid print action.

## 2. Remove standalone printout tabs

### 2.1 User story

As a warehouse user, I want to access printouts from their transaction tabs instead of broad standalone tabs, so a printout remains connected to the relevant transaction.

### 2.2 Acceptance criteria

- **WHEN** a user opens Receiving **THEN** the page **SHALL NOT** show the standalone Receiving Reports tab.
- **WHEN** a user opens Packing **THEN** the page **SHALL NOT** show the standalone Packing Labels tab.
- **WHEN** those tabs are removed **THEN** applicable print workflows **SHALL** remain available from their transaction tabs.

## 3. Print status on Packing tabs

### 3.1 User story

As a warehouse user, I want print-status views on every Packing tab, so I can filter records by whether they need printing or have been printed.

### 3.2 Acceptance criteria

- **WHEN** a user opens any Packing tab—Stock Transfer Request, Returns, or Returns to Supplier—**THEN** the page **SHALL** provide For Printing, Printed, and All views for that tab's records.
- **WHEN** a user selects a status on a Packing tab **THEN** the grid **SHALL** filter records in that tab's transaction context.
- **WHEN** a Packing tab has an existing supported print action **THEN** that action **SHALL** be preserved.

## 4. Accurate Printed counts

### 4.1 User story

As a warehouse user, I want the Printed view and count to reflect print actions, so I can trust the status information across the pages I use.

### 4.2 Acceptance criteria

- **WHEN** the system's print action successfully generates a supported printout **THEN** the record's status **SHALL** be reflected as Printed according to the verified source of print-count data.
- **WHEN** a user opens or refreshes any page using print-status views **THEN** the Printed count **SHALL** correspond to the records matching its applicable transaction, date range, and other active filters.
- **WHEN** a print action does not successfully generate its printout **THEN** the record **SHALL NOT** be reported as printed solely because the user clicked the action.
- **WHEN** the existing external printout does not update the print-count field **THEN** implementation **SHALL** identify and use an appropriate verified mechanism to keep status accurate, without double-counting print actions.

## 5. Preserve existing behavior

### 5.1 Acceptance criteria

- **WHEN** a user switches among the remaining transaction tabs **THEN** existing transaction grids, filters, and navigation **SHALL** continue to work.
- **WHEN** a user prints through an existing supported action **THEN** its destination and record identity **SHALL** be preserved unless a different behavior is explicitly approved.
- **WHEN** a user uses For Printing, Printed, or All **THEN** existing filter meanings **SHALL** be preserved, except for changes needed to make counts accurately reflect printing.

## Scope

### In scope

- Remove the standalone Receiving Reports and Packing Labels tabs.
- Provide For Printing, Printed, and All views on every Packing tab.
- Add a per-record print button to every transaction tab inside Receiving and Packing.
- Preserve existing row-level print actions, including STR Returns and Returns to Supplier.
- Provide the Receiving Report workflow in Purchase Orders and the Packing Label workflow in the relevant Packing transaction context.
- Investigate and correct inaccurate Printed counts across all pages using print-status views.

### Out of scope

- Changing printout templates or unrelated transaction behavior.
- Extending print-status behavior to document types whose print-count field or status source is unverified, including Trip Tickets.
- Broad refactoring of transaction grids, integrations, or authorization.

## Preservation requirements

- Preserve existing transaction-tab content, navigation, and selection behavior.
- Preserve existing supported print actions and their record-specific identifiers.
- Preserve current For Printing and All semantics.
- Avoid counting a record as printed if print generation failed.
- Follow the existing Blazor handler and UI conventions.
