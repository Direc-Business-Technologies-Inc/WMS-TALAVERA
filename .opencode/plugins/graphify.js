// graphify OpenCode plugin — reminder injection DISABLED (see below).
//
// One file for both runtimes. OpenCode 2 reads `id` and `setup()` from the
// default export; OpenCode 1 (1.3.4 and later) reads `server()` from it. Each
// ignores the other's function. Nothing is imported from an opencode package:
// a plain file in .opencode/plugins/ cannot resolve one.
//
// ---------------------------------------------------------------------------
// WHY INJECTION IS OFF
//
// The original hook rewrote the first shell command of a session as
//
//     echo "<reminder>" ; <cmd>
//
// Two problems, both confirmed in practice:
//
//  1. It BREAKS PERMISSION ALLOWLISTS. Tool hooks run *before* the
//     permission check, so the checker saw the rewritten string. A restrictive
//     agent (e.g. @mentor, which allows only `git status`, `dotnet test`,
//     `graphify query`, ...) has no entry for `echo ...`, so even
//     `git status` was denied — and so was the very `graphify query` the
//     reminder was advertising. The nudge actively prevented the behaviour
//     it was nudging toward.
//
//  2. It WAS REDUNDANT. The rules live in the project-root AGENTS.md, which
//     OpenCode auto-loads into every agent working in this repo. The echo
//     repeated instructions the agent already had.
//
// Appending instead of prepending was rejected: in PowerShell the exit status
// would then come from `echo` (always 0), silently reporting failed
// `dotnet build` / `dotnet test` runs as successful. A false green build is a
// far worse defect than one denied command per session.
//
// To restore the nudge (only sensible if an agent exists that does NOT load
// AGENTS.md), set ENABLED = true. Nothing else needs to change.
// ---------------------------------------------------------------------------

import { existsSync } from "fs";
import { join } from "path";

// Flip to true to re-enable the reminder echo. See the note above.
const ENABLED = false;

// ';' not '&&' — Windows PowerShell 5.1 rejects '&&' as a statement
// separator, breaking the first shell command of the session (#1646).
// Keep this string free of backticks and $(...) constructs: it is embedded
// in a double-quoted echo, where backticks would trigger command
// substitution and execute the graphify command we are only suggesting.
const REMINDER =
  'echo "[graphify] knowledge graph at graphify-out/. For focused questions, run graphify query with your question (scoped subgraph, usually much smaller than GRAPH_REPORT.md) instead of grepping raw files. Read GRAPH_REPORT.md only for broad architecture context." ; ';

const hasGraph = (directory) => existsSync(join(directory, "graphify-out", "graph.json"));

// A project and a directory above it (often ~) can each hold a copy of this
// file, and OpenCode loads both. OpenCode 2 fails the second plugin with the
// same id ("Duplicate plugin ID"), so the id carries this file's location. The
// copies share one record of who was reminded, so a command gets one reminder.
const location = [...import.meta.url].reduce((hash, ch) => (Math.imul(hash, 31) + ch.charCodeAt(0)) >>> 0, 7);
const reminded = (globalThis.__graphifyReminded ??= new Set());

export default {
  id: `graphify-${location.toString(16)}`,

  // OpenCode 2: the command tool is "shell", and one server outlives many
  // sessions, so the reminder is tracked per session.
  async setup(ctx) {
    await ctx.tool.hook("execute.before", (event) => {
      if (!ENABLED) return;
      if (event.tool !== "shell" || typeof event.input?.command !== "string") return;
      if (reminded.has(event.sessionID)) return;
      if (!hasGraph(ctx.location.directory)) return;

      event.input = { ...event.input, command: REMINDER + event.input.command };
      reminded.add(event.sessionID);
    });
  },

  // OpenCode 1: the command tool is "bash", and the server lives as long as
  // the session, so the reminder is tracked per project directory.
  async server({ directory }) {
    return {
      "tool.execute.before": async (input, output) => {
        if (!ENABLED) return;
        if (input.tool !== "bash" || typeof output.args?.command !== "string") return;
        if (reminded.has(directory)) return;
        if (!hasGraph(directory)) return;

        output.args.command = REMINDER + output.args.command;
        reminded.add(directory);
      },
    };
  },
};