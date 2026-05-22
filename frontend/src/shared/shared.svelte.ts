import { persisted } from 'svelte-persisted-store';
import type { SavedModInfo } from '../types/mod_summaries';

// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted('selectedMods', {
	selectedMods: {} as { [key: string]: SavedModInfo }
});
