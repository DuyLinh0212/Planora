import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const [projectDirectory, allowlistPath] = process.argv.slice(2);
if (!projectDirectory || !allowlistPath) {
  throw new Error('Usage: node tools/ci/npm-audit.mjs <project-directory> <allowlist.json>');
}

const projectPath = path.resolve(projectDirectory);
const allowlist = JSON.parse(readFileSync(path.resolve(allowlistPath), 'utf8'));
const result = spawnSync('npm', ['audit', '--omit=dev', '--json'], {
  cwd: projectPath,
  encoding: 'utf8',
  shell: process.platform === 'win32',
});

let report;
try {
  report = JSON.parse(result.stdout || '{}');
} catch {
  console.error(result.stdout);
  console.error(result.stderr);
  throw new Error('npm audit did not return valid JSON.');
}

if (report.error) {
  console.error(report.error.summary ?? report.error);
  process.exit(1);
}

const blocking = [];
const accepted = [];
for (const [packageName, vulnerability] of Object.entries(report.vulnerabilities ?? {})) {
  for (const advisory of vulnerability.via ?? []) {
    if (typeof advisory === 'string' || !['high', 'critical'].includes(advisory.severity)) {
      continue;
    }

    const advisoryId = advisory.url?.split('/').filter(Boolean).at(-1);
    const exception = allowlist.exceptions?.[advisoryId];
    const expired = !exception?.expires || Date.parse(`${exception.expires}T23:59:59Z`) < Date.now();
    const wrongPackage = exception?.package !== packageName;
    const finding = `${advisoryId ?? advisory.title} (${advisory.severity}) in ${packageName}`;

    if (!exception || expired || wrongPackage) {
      blocking.push(`${finding}${expired && exception ? ' - exception expired' : ''}`);
    } else {
      accepted.push(`${finding} - temporarily accepted until ${exception.expires}`);
    }
  }
}

for (const finding of accepted) {
  console.warn(`WARNING: ${finding}`);
}

if (blocking.length > 0) {
  console.error('High/critical production vulnerabilities found:');
  for (const finding of blocking) {
    console.error(`- ${finding}`);
  }
  process.exit(1);
}

console.log('Production dependency audit passed.');
