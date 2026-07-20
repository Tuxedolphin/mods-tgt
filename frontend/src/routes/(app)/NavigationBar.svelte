<script lang="ts">
  import {
    Calendar1,
    Home,
    House,
    type LucideIcon,
    Menu,
    Settings,
    Settings2,
    Share,
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

  const navigation_items: NavigationItemProp[] = [
    { icon: House, title: "Home", path: "/home" },
    { icon: Calendar1, title: "Planner", path: "/planner" },
    { icon: Share2, title: "Shared with me", path: "/shared" },
    { icon: UserRound, title: "Profile", path: "/me" },
    { icon: Settings2, title: "Settings", path: "/settings" },
  ];
</script>

<div class="px-2 md:px-0 bg-base-200 shadow-sm">
  <div class="container flex justify-between mx-auto">
    <div class="flex items-center">
      <label for="main-drawer" aria-label="open sidebar">
        <!-- <Menu></Menu> -->
      </label>
      <button
        aria-label="Mods Together Logo"
        onclick={() => {
          goto(resolve("/(app)/home"));
        }}
      >
        <enhanced:img
          class="aspect-5/2 h-14 w-auto align-middle"
          src={mods_tgt_header}
          alt="Mods Together Logo"
        />
      </button>
    </div>

    <div class="flex items-center gap-1">
      <p>@{$currentUserInformation.handle}</p>
      <UserAvatar></UserAvatar>
    </div>
  </div>
</div>
<div class="container mx-auto">
  <div class="flex items-center justify-between w-full md:hidden px-2 pt-1">
    {#each navigation_items as item}
      <NavItemLargeScreen information={item}></NavItemLargeScreen>
    {/each}
  </div>
</div>
<div class="container mx-auto px-2 md:px-0">
  <div class="flex">
    <div class="md:flex flex-col gap-1 hidden md:min-w-16 xl:min-w-48 w-4 mt-4">
      {#each navigation_items as item}
        <NavItemLargeScreen information={item}></NavItemLargeScreen>
      {/each}
    </div>
    <div class="w-full">
      {@render children()}
    </div>
  </div>
</div>
