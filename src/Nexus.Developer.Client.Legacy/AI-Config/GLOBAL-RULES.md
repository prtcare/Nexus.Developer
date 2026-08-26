\# NEXUS GLOBAL DEVELOPMENT RULES



These rules apply to all Nexus development repositories.



\## ROLE



Claude is the planning, architecture and review agent.



DeepSeek running through Claude Code is primarily the implementation agent.



\## WORKING PRINCIPLE



Do not ask the user questions that can be answered by:



1\. Reading the repository.

2\. Reading project documentation.

3\. Reading the current development status.

4\. Reading existing architecture.

5\. Inspecting existing code.

6\. Running builds or tests.



Routine implementation decisions should be handled autonomously.



\## SOURCE OF TRUTH PRIORITY



When instructions conflict, use this priority:



1\. Explicit current task instruction

2\. NEXUS\_DEVELOPMENT\_CONTROL.xlsx — Master Roadmap, Active Changes, Session Protocol \(at `C:\Personal\Nexus.Developer`\), and its `control/` mirrors when the workbook is unavailable

3\. Repository AGENTS.md \(per-repo governance, e.g. `Nexus.Developer\AGENTS.md`\)

4\. CURRENT-DEVELOPMENT.md / MILESTONE-MASTER.md, where a repository still has them

5\. Nexus architecture documentation \(NEXUS\_MASTER\_ARCHITECTURE.md, nexus-roadmap.yaml\)

6\. Existing implementation

7\. General assumptions



\*Updated 2026-08-26 \(CHG-20260826-002\): the workbook is the current governed system of\
record for Layer 07 until `M-07-1.1` replaces it with the permanent work graph. It now\
outranks CURRENT-DEVELOPMENT.md/MILESTONE-MASTER.md, which predate it and are not\
guaranteed to exist in every repository.\*



If a serious contradiction still exists, STOP and escalate.



\## NEVER GUESS ARCHITECTURE



Do not silently make major architecture decisions.



Stop if a decision affects:



\- repository boundaries

\- Nexus layers

\- public APIs

\- database architecture

\- authentication

\- security

\- infrastructure

\- deployment

\- shared packages

\- common Product Core

\- cross-layer contracts



\## NEVER DESTROY AUTOMATICALLY



Never automatically:



\- delete repositories

\- delete branches

\- force push

\- rewrite git history

\- drop databases

\- drop tables

\- delete production resources

\- change production secrets

\- deploy to production

