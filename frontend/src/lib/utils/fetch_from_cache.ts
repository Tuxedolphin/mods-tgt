import { Err, Ok, type Result } from 'ts-results-es'
import type { ModSummary } from '../types/mod_summaries'
import type { Module } from '../types/modules'

export function getFromSessionStorage<T>(cacheKey: string): Result<T, null> {
	const uniqueKey = `Zhun:${cacheKey}`
	const item = sessionStorage.getItem(uniqueKey)
	if (item) {
		return Ok(JSON.parse(item))
	}
	return Err(null)
}

export function storeInfoSessionStorage<T>(cacheKey: string, item: T) {
	const uniqueKey = `Zhun:${cacheKey}`
	const json = JSON.stringify(item)
	sessionStorage.setItem(uniqueKey, json)
}

// Fetches from session cache (between tabs), but
// if not found, fetches using Fetch link and stores it in
// session cache:
async function getFromCache<T>(cacheKey: string, fetchLink: string): Promise<T> {
	const res = getFromSessionStorage(cacheKey)
	if (res.isOk()) {
		return res.value as T
	}
	const fetchFromLink = await fetch(fetchLink)
	const resJson = await fetchFromLink.json()
	storeInfoSessionStorage(cacheKey, resJson)
	return resJson
}

export async function getListOfModsSummary(acadYear: string): Promise<ModSummary[]> {
	return getFromCache<ModSummary[]>(
		`modSummary-${acadYear}`,
		`https://api.nusmods.com/v2/${acadYear}/moduleList.json`
	)
}

export async function getFullModInfo(moduleCode: string, acadYear: string): Promise<Module> {
	return getFromCache<Module>(
		`${moduleCode}-${acadYear}`,
		`https://api.nusmods.com/v2/${acadYear}/modules/${moduleCode}.json`
	)
}
