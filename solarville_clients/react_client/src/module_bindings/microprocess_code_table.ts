// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN YOUR MODULE SOURCE CODE INSTEAD.

/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
import {
  AlgebraicType,
  AlgebraicValue,
  BinaryReader,
  BinaryWriter,
  CallReducerFlags,
  ConnectionId,
  DbConnectionBuilder,
  DbConnectionImpl,
  DbContext,
  ErrorContextInterface,
  Event,
  EventContextInterface,
  Identity,
  ProductType,
  ProductTypeElement,
  ReducerEventContextInterface,
  SubscriptionBuilderImpl,
  SubscriptionEventContextInterface,
  SumType,
  SumTypeVariant,
  TableCache,
  TimeDuration,
  Timestamp,
  deepEqual,
} from "@clockworklabs/spacetimedb-sdk";
import { MicroprocessCode } from "./microprocess_code_type";
import { EventContext, Reducer, RemoteReducers, RemoteTables } from ".";

/**
 * Table handle for the table `microprocess_code`.
 *
 * Obtain a handle from the [`microprocessCode`] property on [`RemoteTables`],
 * like `ctx.db.microprocessCode`.
 *
 * Users are encouraged not to explicitly reference this type,
 * but to directly chain method calls,
 * like `ctx.db.microprocessCode.on_insert(...)`.
 */
export class MicroprocessCodeTableHandle {
  tableCache: TableCache<MicroprocessCode>;

  constructor(tableCache: TableCache<MicroprocessCode>) {
    this.tableCache = tableCache;
  }

  count(): number {
    return this.tableCache.count();
  }

  iter(): Iterable<MicroprocessCode> {
    return this.tableCache.iter();
  }
  /**
   * Access to the `code_id` unique index on the table `microprocess_code`,
   * which allows point queries on the field of the same name
   * via the [`MicroprocessCodeCodeIdUnique.find`] method.
   *
   * Users are encouraged not to explicitly reference this type,
   * but to directly chain method calls,
   * like `ctx.db.microprocessCode.code_id().find(...)`.
   *
   * Get a handle on the `code_id` unique index on the table `microprocess_code`.
   */
  code_id = {
    // Find the subscribed row whose `code_id` column value is equal to `col_val`,
    // if such a row is present in the client cache.
    find: (col_val: number): MicroprocessCode | undefined => {
      for (let row of this.tableCache.iter()) {
        if (deepEqual(row.code_id, col_val)) {
          return row;
        }
      }
    },
  };

  onInsert = (cb: (ctx: EventContext, row: MicroprocessCode) => void) => {
    return this.tableCache.onInsert(cb);
  }

  removeOnInsert = (cb: (ctx: EventContext, row: MicroprocessCode) => void) => {
    return this.tableCache.removeOnInsert(cb);
  }

  onDelete = (cb: (ctx: EventContext, row: MicroprocessCode) => void) => {
    return this.tableCache.onDelete(cb);
  }

  removeOnDelete = (cb: (ctx: EventContext, row: MicroprocessCode) => void) => {
    return this.tableCache.removeOnDelete(cb);
  }

  // Updates are only defined for tables with primary keys.
  onUpdate = (cb: (ctx: EventContext, oldRow: MicroprocessCode, newRow: MicroprocessCode) => void) => {
    return this.tableCache.onUpdate(cb);
  }

  removeOnUpdate = (cb: (ctx: EventContext, onRow: MicroprocessCode, newRow: MicroprocessCode) => void) => {
    return this.tableCache.removeOnUpdate(cb);
  }}
