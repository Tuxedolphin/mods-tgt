<script lang="ts">
	import type { PageProps } from './$types';
	let { data }: PageProps = $props();
	
	import SearchBar from '../components/SearchBar.svelte';

	// const currentDay = new Date().getDay();
	// Get Localised Sunday to Sat:
	const daysOfWeek = [
		new Date('25 Jan 2026'), // Sunday
		new Date('26 Jan 2026'),
		new Date('27 Jan 2026'),
		new Date('28 Jan 2026'),
		new Date('29 Jan 2026'),
		new Date('30 Jan 2026'),
		new Date('31 Jan 2026') // Monday
	];


	const dateFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'short' });

    const timeFormatter = new Intl.DateTimeFormat(undefined, {
      hour: '2-digit',
      minute: '2-digit'
    });


	function formatShortDate(date: Date): string {
		return dateFormatter.format(date);
	}

    // eslint-disable-next-line svelte/prefer-svelte-reactivity
    const dateTimeToSet = new Date('25 Jan 2026');

    function formatShortTime(hour: number): string {
        return timeFormatter.format(dateTimeToSet.setHours(hour, 0, 0, 0))
    }

</script>
<SearchBar summaries={data.data}></SearchBar>
<div class="flex">
	<div class="row-auto w-12 flex-initial">
        <div class="h-16"></div>
		{#each { length: 13 }, i}
			<div class="h-16 text-center align-middle">
                <p class="relative bottom-3">{formatShortTime(i + 8)}</p>
            </div>
		{/each}
	</div>
	<div class="flex-1">
		<div class="columns-5 text-center gap-2">
			{#each { length: 5 }, i}
				<div class="h-16 border-2">{formatShortDate(daysOfWeek[i + 1])}</div>
				{#each { length: 12 }, i}
                    {#if i % 2 == 0}
					    <div class="h-16 text-center bg-base-200">{i}</div>
                    {:else}
					    <div class="h-16 text-center bg-base-300">{i}</div>
                    {/if}
                    
				{/each}
			{/each}
		</div>
	</div>
</div>
