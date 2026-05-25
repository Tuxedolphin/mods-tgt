import type { ModSummary } from '../types/mod_summaries';
import type { Module } from '../types/modules';

// Fetches from session cache (between tabs), but
// if not found, fetches using Fetch link and stores it in
// session cache:
async function getFromCache<T>(cacheKey: string, fetchLink: string): Promise<T> {
	const uniqueKey = `Zhun:${cacheKey}`;
	let item = sessionStorage.getItem(uniqueKey);

	if (!item) {
		const res = await fetch(fetchLink);
		const resJson = await res.json();
		item = JSON.stringify(resJson);
		sessionStorage.setItem(uniqueKey, item);
	}

	return JSON.parse(item);
}

export async function getListOfModsSummary(): Promise<ModSummary[]> {
	return getFromCache<ModSummary[]>(
		'modSummary',
		'https://api.nusmods.com/v2/2025-2026/moduleList.json'
	);
}

export async function getFullModInfo(moduleCode: string): Promise<Module> {
	return getFromCache<Module>(
		moduleCode,
		`https://api.nusmods.com/v2/2025-2026/modules/${moduleCode}.json`
	);
}
