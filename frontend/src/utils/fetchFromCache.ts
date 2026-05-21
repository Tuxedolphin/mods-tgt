import type { Module } from "../types/modules";

export async function getFullModInfo(moduleCode: string): Promise<Module> {
    let item = sessionStorage.getItem(moduleCode);

    // If item does not exist, fetch it. 
    if (!item) {
		const modInfo = await fetch(`https://api.nusmods.com/v2/2025-2026/modules/${moduleCode}.json`);
        const modInfoJson = await modInfo.json();
        item = JSON.stringify(modInfoJson);
        sessionStorage.setItem(moduleCode, item);
    }

    return JSON.parse(item);
}