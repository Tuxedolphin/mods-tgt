<script lang="ts">
  import {
    Calendar1,
    Heart,
    House,
    Settings2,
    Share2,
    UserRound,
  } from "@lucide/svelte";
  import mods_tgt_header from "$lib/assets/mods_tgt_header.png?enhanced";
  import type { LayoutProps } from "../$types";
  import NavItemLargeScreen from "./NavItemLargeScreen.svelte";
  import UserAvatar from "./UserAvatar.svelte";

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  let { children }: LayoutProps = $props();

  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type { NavigationItemProp } from "$lib/types/internal";
  import {
    PUBLIC_VERSION_NUMBER,
    PUBLIC_GITHUB_SHA_VALUE,
  } from "$env/static/public";

  const navigation_items: NavigationItemProp[] = [
    { icon: House, title: "Home", path: "/home" },
    { icon: Calendar1, title: "Planner", path: "/planner" },
    { icon: Share2, title: "Shared with me", path: "/shared" },
    { icon: UserRound, title: "Profile", path: "/me" },
    { icon: Settings2, title: "Settings", path: "/settings" },
    { icon: Heart, title: "Support", path: "/support" },
  ];
</script>

<div class="flex flex-col min-h-screen">
  <div class="px-2 md:px-0 bg-base-200 shadow-sm grow-0">
    <div class="container flex justify-between mx-auto">
      <div class="flex items-center">
        <enhanced:img
          class="aspect-5/2 h-14 w-auto align-middle"
          src={mods_tgt_header}
        />
      </div>

      <div class="flex items-center gap-1">
        <p>@{$currentUserInformation.handle}</p>
        <UserAvatar></UserAvatar>
      </div>
    </div>

    <div>
      <div class="flex items-center justify-between w-full md:hidden px-2 pt-1">
        {#each navigation_items as item}
          <NavItemLargeScreen information={item}></NavItemLargeScreen>
        {/each}
      </div>
    </div>
  </div>

  <div class="container mx-auto px-2 md:px-0 grow">
    <div class="flex flex-col w-full">
      <div class="flex">
        <div
          class="md:flex flex-col gap-1 hidden md:min-w-16 xl:min-w-48 w-4 mt-4"
        >
          {#each navigation_items as item}
            <NavItemLargeScreen information={item}></NavItemLargeScreen>
          {/each}
        </div>
        <div class="flex-1 overflow-auto">
          {@render children()}
        </div>
      </div>
    </div>
  </div>

  <div class="bg-base-200">
    <footer class="grow-0 p-8 container mx-auto text-center">
      <div class="p-2">
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <!-- svelte-ignore a11y_missing_attribute -->
        <a
          onclick={() => {
            goto(resolve("/(app)/report-issue"));
          }}
          class="link link-primary link-hover"
          target="_blank">Report an Issue</a
        >
        •
        <a
          href="https://github.com/Tuxedolphin/mods-tgt"
          class="link link-primary link-hover"
          target="_blank">Github</a
        >
        •
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <!-- svelte-ignore a11y_missing_attribute -->
        <a
          onclick={() => {
            goto(resolve("/(app)/credits"));
          }}
          class="link link-primary link-hover"
          target="_blank">Credits & Attributions</a
        >
      </div>
      <div>
        Module Data provided by the amazing team behind <a
          class="link link-primary link-hover"
          href="https://nusmods.com/"
          target="_blank">NUSMods</a
        >
        through the
        <a
          href="https://api.nusmods.com/v2/"
          class="link link-primary link-hover"
          target="_blank">NUSMods API</a
        >
      </div>
      <div>
        Built with much ❤️ by <a
          href="https://github.com/Tuxedolphin"
          class="link link-primary link-hover"
          target="_blank">Zhuzhen</a
        >
        and
        <a
          href="https://github.com/Jyodann"
          class="link link-primary link-hover"
          target="_blank">Jordan</a
        >
        for
        <a
          href="https://nusskylab-v2-dev.comp.nus.edu.sg/projects"
          class="link link-primary link-hover"
          target="_blank">Orbital 2026</a
        >
      </div>

      <div>
        Copyright © 2026, Zhun • ModsTogether v{PUBLIC_VERSION_NUMBER}
        (<a
          class="link link-primary link-hover"
          target="_blank"
          href="https://github.com/Tuxedolphin/mods-tgt/commit/{PUBLIC_GITHUB_SHA_VALUE}"
        >
          {PUBLIC_GITHUB_SHA_VALUE.slice(0, 7)}
        </a>)
      </div>
    </footer>
  </div>
</div>

<!-- Header -->
