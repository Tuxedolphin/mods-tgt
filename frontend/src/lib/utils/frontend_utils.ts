import { Err, Ok, Result } from 'ts-results-es'

export function sleep(ms: number) {
	return new Promise((resolve) => setTimeout(resolve, ms))
}

export function json_tryparse<T>(text: string): Result<T, string> {
	try {
		return Ok(JSON.parse(text))
	} catch {
		return Err(text)
	}
}
